// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using osu.Framework.Allocation;
using osu.Framework.Extensions.ListExtensions;
using osu.Framework.Extensions.TypeExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Input.Events;
using osu.Framework.Input.Handlers;
using osu.Framework.Input.StateChanges;
using osu.Framework.Input.StateChanges.Events;
using osu.Framework.Input.States;
using osu.Framework.Lists;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osu.Framework.Statistics;
using osuTK;
using osuTK.Input;
using JoystickState = osu.Framework.Input.States.JoystickState;
using KeyboardState = osu.Framework.Input.States.KeyboardState;
using MouseState = osu.Framework.Input.States.MouseState;

namespace osu.Framework.Input
{
    public abstract partial class InputManager : Container, IInputStateChangeHandler
    {
        /// <summary>
        /// The initial delay before key repeat begins.
        /// </summary>
        private const int repeat_initial_delay = 250;

        /// <summary>
        /// The delay between key repeats after the initial repeat.
        /// </summary>
        private const int repeat_tick_rate = 70;

        [Resolved(CanBeNull = true)]
        protected GameHost Host { get; private set; }

        /// <summary>
        /// The currently focused <see cref="Drawable"/>. Null if there is no current focus.
        /// </summary>
        public Drawable FocusedDrawable { get; internal set; }

        protected abstract ImmutableArray<InputHandler> InputHandlers { get; }

        private double keyboardRepeatTime;
        private Key? keyboardRepeatKey;

        /// <summary>
        /// The initial input state. <see cref="CurrentState"/> is always equal (as a reference) to the value returned from this.
        /// </summary>
        protected virtual InputState CreateInitialState() => new InputState(
            new MouseState { IsPositionValid = false },
            new KeyboardState(),
            new TouchState(),
            new JoystickState(),
            new MidiState(),
            new TabletState()
        );

        /// <summary>
        /// The last processed state.
        /// </summary>
        public readonly InputState CurrentState;

        /// <summary>
        /// The <see cref="Drawable"/> which is currently being dragged. null if none is.
        /// </summary>
        public Drawable DraggedDrawable
        {
            get
            {
                mouseButtonEventManagers.TryGetValue(MouseButton.Left, out var manager);
                return manager?.DraggedDrawable;
            }
        }

        /// <summary>
        /// Contains the previously hovered <see cref="Drawable"/>s prior to when
        /// <see cref="hoveredDrawables"/> got updated.
        /// </summary>
        private readonly List<Drawable> lastHoveredDrawables = new List<Drawable>();

        /// <summary>
        /// Contains all hovered <see cref="Drawable"/>s in top-down order up to the first
        /// which returned true in its <see cref="Drawable.OnHover"/> method.
        /// Top-down in this case means reverse draw order, i.e. the front-most visible
        /// <see cref="Drawable"/> first, and <see cref="Container"/>s after their children.
        /// </summary>
        private readonly List<Drawable> hoveredDrawables = new List<Drawable>();

        /// <summary>
        /// The <see cref="Drawable"/> which returned true in its
        /// <see cref="Drawable.OnHover"/> method, or null if none did so.
        /// </summary>
        private Drawable hoverHandledDrawable;

        /// <summary>
        /// Contains all hovered <see cref="Drawable"/>s in top-down order up to the first
        /// which returned true in its <see cref="Drawable.OnHover"/> method.
        /// Top-down in this case means reverse draw order, i.e. the front-most visible
        /// <see cref="Drawable"/> first, and <see cref="Container"/>s after their children.
        /// </summary>
        public SlimReadOnlyListWrapper<Drawable> HoveredDrawables => hoveredDrawables.AsSlimReadOnly();

        /// <summary>
        /// Contains all <see cref="Drawable"/>s in top-down order which are considered
        /// for positional input. This list is the same as <see cref="HoveredDrawables"/>, only
        /// that the return value of <see cref="Drawable.OnHover"/> is not taken
        /// into account.
        /// </summary>
        /// <remarks>
        /// This collection should not be retained as a reference. The contents is not stable outside of local usage.
        /// </remarks>
        public SlimReadOnlyListWrapper<Drawable> PositionalInputQueue => buildPositionalInputQueue(CurrentState.Mouse.Position);

        /// <summary>
        /// Contains all <see cref="Drawable"/>s in top-down order which are considered
        /// for non-positional input.
        /// </summary>
        /// <remarks>
        /// This collection should not be retained as a reference. The contents is not stable outside of local usage.
        /// </remarks>
        public SlimReadOnlyListWrapper<Drawable> NonPositionalInputQueue => buildNonPositionalInputQueue();

        private readonly Dictionary<MouseButton, MouseButtonEventManager> mouseButtonEventManagers = new Dictionary<MouseButton, MouseButtonEventManager>();
        private readonly Dictionary<Key, KeyEventManager> keyButtonEventManagers = new Dictionary<Key, KeyEventManager>();
        private readonly Dictionary<TouchSource, TouchEventManager> touchEventManagers = new Dictionary<TouchSource, TouchEventManager>();
        private readonly Dictionary<TabletPenButton, TabletPenButtonEventManager> tabletPenButtonEventManagers = new Dictionary<TabletPenButton, TabletPenButtonEventManager>();
        private readonly Dictionary<TabletAuxiliaryButton, TabletAuxiliaryButtonEventManager> tabletAuxiliaryButtonEventManagers = new Dictionary<TabletAuxiliaryButton, TabletAuxiliaryButtonEventManager>();
        private readonly Dictionary<JoystickButton, JoystickButtonEventManager> joystickButtonEventManagers = new Dictionary<JoystickButton, JoystickButtonEventManager>();
        private readonly Dictionary<MidiKey, MidiKeyEventManager> midiKeyEventManagers = new Dictionary<MidiKey, MidiKeyEventManager>();

        private readonly Dictionary<JoystickAxisSource, JoystickAxisEventManager> joystickAxisEventManagers = new Dictionary<JoystickAxisSource, JoystickAxisEventManager>();

        /// <summary>
        /// Whether to produce mouse input on any touch input from latest source.
        /// </summary>
        protected virtual bool MapMouseToLatestTouch => true;

        protected InputManager()
        {
            CurrentState = CreateInitialState();
            RelativeSizeAxes = Axes.Both;

            foreach (var button in Enum.GetValues<MouseButton>())
            {
                var manager = CreateButtonEventManagerFor(button);
                manager.RequestFocus = ChangeFocusFromClick;
                manager.GetInputQueue = () => PositionalInputQueue;
                manager.GetCurrentTime = () => Time.Current;
                mouseButtonEventManagers.Add(button, manager);
            }
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            // Set mouse position to zero in input manager local space instead of screen space zero.
            // This ensures initial mouse position is non-negative in nested input managers whose origin is not (0, 0).
            CurrentState.Mouse.Position = ToScreenSpace(Vector2.Zero);
        }

        /// <summary>
        /// Create a <see cref="MouseButtonEventManager"/> for a specified mouse button.
        /// </summary>
        /// <param name="button">The button to be handled by the returned manager.</param>
        /// <returns>The <see cref="MouseButtonEventManager"/>.</returns>
        protected virtual MouseButtonEventManager CreateButtonEventManagerFor(MouseButton button)
        {
            switch (button)
            {
                case MouseButton.Left:
                    return new MouseLeftButtonEventManager(button);

                default:
                    return new MouseMinorButtonEventManager(button);
            }
        }

        /// <summary>
        /// Get the <see cref="MouseButtonEventManager"/> responsible for a specified mouse button.
        /// </summary>
        /// <param name="button">The button to find the manager for.</param>
        /// <returns>The <see cref="MouseButtonEventManager"/>.</returns>
        public MouseButtonEventManager GetButtonEventManagerFor(MouseButton button) =>
            mouseButtonEventManagers.TryGetValue(button, out var manager) ? manager : null;

        /// <summary>
        /// Create a <see cref="KeyEventManager"/> for a specified key.
        /// </summary>
        /// <param name="key">The key to be handled by the returned manager.</param>
        /// <returns>The <see cref="KeyEventManager"/>.</returns>
        protected virtual KeyEventManager CreateButtonEventManagerFor(Key key) => new KeyEventManager(key);

        /// <summary>
        /// Get the <see cref="KeyEventManager"/> responsible for a specified key.
        /// </summary>
        /// <param name="key">The key to find the manager for.</param>
        /// <returns>The <see cref="KeyEventManager"/>.</returns>
        public KeyEventManager GetButtonEventManagerFor(Key key)
        {
            if (keyButtonEventManagers.TryGetValue(key, out var existing))
                return existing;

            var manager = CreateButtonEventManagerFor(key);
            manager.GetInputQueue = () => NonPositionalInputQueue;
            return keyButtonEventManagers[key] = manager;
        }

        /// <summary>
        /// Create a <see cref="TouchEventManager"/> for a specified touch source.
        /// </summary>
        /// <param name="source">The touch source to be handled by the returned manager.</param>
        /// <returns>The <see cref="TouchEventManager"/>.</returns>
        protected virtual TouchEventManager CreateButtonEventManagerFor(TouchSource source) => new TouchEventManager(source);

        /// <summary>
        /// Get the <see cref="TouchEventManager"/> responsible for a specified touch source.
        /// </summary>
        /// <param name="source">The touch source to find the manager for.</param>
        /// <returns>The <see cref="TouchEventManager"/>.</returns>
        public TouchEventManager GetButtonEventManagerFor(TouchSource source)
        {
            if (touchEventManagers.TryGetValue(source, out var existing))
                return existing;

            var manager = CreateButtonEventManagerFor(source);
            manager.GetInputQueue = () => buildPositionalInputQueue(CurrentState.Touch.TouchPositions[(int)source]);
            return touchEventManagers[source] = manager;
        }

        /// <summary>
        /// Create a <see cref="TabletPenButtonEventManager"/> for a specified tablet pen button.
        /// </summary>
        /// <param name="button">The button to be handled by the returned manager.</param>
        /// <returns>The <see cref="TabletPenButtonEventManager"/>.</returns>
        protected virtual TabletPenButtonEventManager CreateButtonEventManagerFor(TabletPenButton button) => new TabletPenButtonEventManager(button);

        /// <summary>
        /// Get the <see cref="TabletPenButtonEventManager"/> responsible for a specified tablet pen button.
        /// </summary>
        /// <param name="button">The button to find the manager for.</param>
        /// <returns>The <see cref="TabletPenButtonEventManager"/>.</returns>
        public TabletPenButtonEventManager GetButtonEventManagerFor(TabletPenButton button)
        {
            if (tabletPenButtonEventManagers.TryGetValue(button, out var existing))
                return existing;

            var manager = CreateButtonEventManagerFor(button);
            manager.GetInputQueue = () => PositionalInputQueue;
            return tabletPenButtonEventManagers[button] = manager;
        }

        /// <summary>
        /// Create a <see cref="TabletAuxiliaryButtonEventManager"/> for a specified tablet auxiliary button.
        /// </summary>
        /// <param name="button">The button to be handled by the returned manager.</param>
        /// <returns>The <see cref="TabletAuxiliaryButtonEventManager"/>.</returns>
        protected virtual TabletAuxiliaryButtonEventManager CreateButtonEventManagerFor(TabletAuxiliaryButton button) => new TabletAuxiliaryButtonEventManager(button);

        /// <summary>
        /// Get the <see cref="TabletAuxiliaryButtonEventManager"/> responsible for a specified tablet auxiliary button.
        /// </summary>
        /// <param name="button">The button to find the manager for.</param>
        /// <returns>The <see cref="TabletAuxiliaryButtonEventManager"/>.</returns>
        public TabletAuxiliaryButtonEventManager GetButtonEventManagerFor(TabletAuxiliaryButton button)
        {
            if (tabletAuxiliaryButtonEventManagers.TryGetValue(button, out var existing))
                return existing;

            var manager = CreateButtonEventManagerFor(button);
            manager.GetInputQueue = () => NonPositionalInputQueue;
            return tabletAuxiliaryButtonEventManagers[button] = manager;
        }

        /// <summary>
        /// Create a <see cref="JoystickButtonEventManager"/> for a specified joystick button.
        /// </summary>
        /// <param name="button">The button to be handled by the returned manager.</param>
        /// <returns>The <see cref="JoystickButtonEventManager"/>.</returns>
        protected virtual JoystickButtonEventManager CreateButtonEventManagerFor(JoystickButton button) => new JoystickButtonEventManager(button);

        /// <summary>
        /// Get the <see cref="JoystickButtonEventManager"/> responsible for a specified joystick button.
        /// </summary>
        /// <param name="button">The button to find the manager for.</param>
        /// <returns>The <see cref="JoystickButtonEventManager"/>.</returns>
        public JoystickButtonEventManager GetButtonEventManagerFor(JoystickButton button)
        {
            if (joystickButtonEventManagers.TryGetValue(button, out var existing))
                return existing;

            var manager = CreateButtonEventManagerFor(button);
            manager.GetInputQueue = () => NonPositionalInputQueue;
            return joystickButtonEventManagers[button] = manager;
        }

        /// <summary>
        /// Create a <see cref="MidiKeyEventManager"/> for a specified midi key.
        /// </summary>
        /// <param name="key">The key to be handled by the returned manager.</param>
        /// <returns>The <see cref="MidiKeyEventManager"/>.</returns>
        protected virtual MidiKeyEventManager CreateButtonEventManagerFor(MidiKey key) => new MidiKeyEventManager(key);

        /// <summary>
        /// Get the <see cref="MidiKeyEventManager"/> responsible for a specified midi key.
        /// </summary>
        /// <param name="key">The key to find the manager for.</param>
        /// <returns>The <see cref="MidiKeyEventManager"/>.</returns>
        public MidiKeyEventManager GetButtonEventManagerFor(MidiKey key)
        {
            if (midiKeyEventManagers.TryGetValue(key, out var existing))
                return existing;

            var manager = CreateButtonEventManagerFor(key);
            manager.GetInputQueue = () => NonPositionalInputQueue;
            return midiKeyEventManagers[key] = manager;
        }

        /// <summary>
        /// Create a <see cref="JoystickAxisEventManager"/> for a specified joystick axis.
        /// </summary>
        /// <param name="source">The axis to be handled by the returned manager.</param>
        /// <returns>The <see cref="JoystickAxisEventManager"/>.</returns>
        protected virtual JoystickAxisEventManager CreateJoystickAxisEventManagerFor(JoystickAxisSource source) => new JoystickAxisEventManager(source);

        /// <summary>
        /// Get the <see cref="JoystickAxisEventManager"/> responsible for a specified joystick axis.
        /// </summary>
        /// <param name="source">The axis to find the manager for.</param>
        /// <returns>The <see cref="JoystickAxisEventManager"/>.</returns>
        public JoystickAxisEventManager GetJoystickAxisEventManagerFor(JoystickAxisSource source)
        {
            if (joystickAxisEventManagers.TryGetValue(source, out var existing))
                return existing;

            var manager = CreateJoystickAxisEventManagerFor(source);
            manager.GetInputQueue = () => NonPositionalInputQueue;
            return joystickAxisEventManagers[source] = manager;
        }

        /// <summary>
        /// Reset current focused drawable to the top-most drawable which is <see cref="Drawable.RequestsFocus"/>.
        /// </summary>
        /// <param name="triggerSource">The source which triggered this event.</param>
        public void TriggerFocusContention(Drawable triggerSource)
        {
            if (FocusedDrawable == null) return;

            Logger.Log($"Focus contention triggered by {triggerSource}.");
            ChangeFocus(null);
        }

        /// <summary>
        /// Changes the currently-focused drawable. First checks that <paramref name="potentialFocusTarget"/> is in a valid state to receive focus,
        /// then unfocuses the current <see cref="FocusedDrawable"/> and focuses <paramref name="potentialFocusTarget"/>.
        /// <paramref name="potentialFocusTarget"/> can be null to reset focus.
        /// If the given drawable is already focused, nothing happens and no events are fired.
        /// </summary>
        /// <param name="potentialFocusTarget">The drawable to become focused.</param>
        /// <returns>True if the given drawable is now focused (or focus is dropped in the case of a null target).</returns>
        public bool ChangeFocus(Drawable potentialFocusTarget) => ChangeFocus(potentialFocusTarget, CurrentState);

        /// <summary>
        /// Changes the currently-focused drawable. First checks that <paramref name="potentialFocusTarget"/> is in a valid state to receive focus,
        /// then unfocuses the current <see cref="FocusedDrawable"/> and focuses <paramref name="potentialFocusTarget"/>.
        /// <paramref name="potentialFocusTarget"/> can be null to reset focus.
        /// If the given drawable is already focused, nothing happens and no events are fired.
        /// </summary>
        /// <param name="potentialFocusTarget">The drawable to become focused.</param>
        /// <param name="state">The <see cref="InputState"/> associated with the focusing event.</param>
        /// <returns>True if the given drawable is now focused (or focus is dropped in the case of a null target).</returns>
        protected bool ChangeFocus(Drawable potentialFocusTarget, InputState state)
        {
            if (potentialFocusTarget == FocusedDrawable)
                return true;

            if (potentialFocusTarget != null && (!isDrawableValidForFocus(potentialFocusTarget) || !potentialFocusTarget.AcceptsFocus))
                return false;

            var previousFocus = FocusedDrawable;

            FocusedDrawable = null;

            if (previousFocus != null)
            {
                previousFocus.HasFocus = false;
                previousFocus.TriggerEvent(new FocusLostEvent(state, potentialFocusTarget));

                if (FocusedDrawable != null) throw new InvalidOperationException($"Focus cannot be changed inside {nameof(OnFocusLost)}");
            }

            FocusedDrawable = potentialFocusTarget;

            Logger.Log($"Focus changed from {previousFocus?.ToString() ?? "nothing"} to {FocusedDrawable?.ToString() ?? "nothing"}.", LoggingTarget.Runtime, LogLevel.Debug);

            if (FocusedDrawable != null)
            {
                FocusedDrawable.HasFocus = true;
                FocusedDrawable.TriggerEvent(new FocusEvent(state, previousFocus));
            }

            return true;
        }

        internal override bool BuildNonPositionalInputQueue(List<Drawable> queue, bool allowBlocking = true)
        {
            if (!allowBlocking)
                base.BuildNonPositionalInputQueue(queue, false);

            return false;
        }

        internal override bool BuildPositionalInputQueue(Vector2 screenSpacePos, List<Drawable> queue) => false;

        private bool hoverEventsUpdated;

        private readonly List<Drawable> highFrequencyDrawables = new List<Drawable>();

        private MouseMoveEvent highFrequencyMoveEvent;

        protected override void Update()
        {
            unfocusIfNoLongerValid();

            // aggressively clear to avoid holding references.
            inputQueue.Clear();
            positionalInputQueue.Clear();

            hoverEventsUpdated = false;

            var pendingInputs = GetPendingInputs();

            foreach (var result in pendingInputs)
            {
                result.Apply(CurrentState, this);
            }

            if (CurrentState.Mouse.IsPositionValid)
            {
                Debug.Assert(highFrequencyDrawables.Count == 0);

                foreach (var d in PositionalInputQueue)
                {
                    if (d is IRequireHighFrequencyMousePosition)
                        highFrequencyDrawables.Add(d);
                }

                if (highFrequencyDrawables.Count > 0)
                {
                    // conditional avoid allocs of MouseMoveEvent when state is guaranteed to not have been mutated.
                    // can be removed if we pool/change UIEvent allocation to be more efficient.
                    if (highFrequencyMoveEvent == null || pendingInputs.Count > 0)
                        highFrequencyMoveEvent = new MouseMoveEvent(CurrentState);

                    PropagateBlockableEvent(highFrequencyDrawables.AsSlimReadOnly(), highFrequencyMoveEvent);
                }

                highFrequencyDrawables.Clear();
            }

            updateKeyRepeat(CurrentState);

            // Other inputs or drawable changes may affect hover even if
            // there were no mouse movements, so it must be updated every frame.
            if (!hoverEventsUpdated)
                updateHoverEvents(CurrentState);

            if (FocusedDrawable == null)
                focusTopMostRequestingDrawable();

            base.Update();
        }

        private void updateKeyRepeat(InputState state)
        {
            if (!(keyboardRepeatKey is Key key)) return;

            keyboardRepeatTime -= Time.Elapsed;

            while (keyboardRepeatTime < 0)
            {
                GetButtonEventManagerFor(key).HandleRepeat(state);
                keyboardRepeatTime += repeat_tick_rate;
            }
        }

        private readonly List<IInput> inputs = new List<IInput>();

        private readonly List<IInput> dequeuedInputs = new List<IInput>();

        private InputHandler mouseSource;

        private double mouseSourceDebounceTimeRemaining;

        private double lastPendingInputRetrievalTime;

        /// <summary>
        /// The length in time after which a lower priority input handler is allowed to take over mouse control from a high priority handler that is no longer reporting.
        /// It's safe to assume that all input devices are reporting at higher than the debounce time specified here (20hz).
        /// If the delay seen between device swaps is ever considered to be too slow, this can likely be further increased up to 100hz.
        /// </summary>
        private const int mouse_source_debounce_time = 50;

        protected virtual List<IInput> GetPendingInputs()
        {
            double now = Clock.CurrentTime;
            double elapsed = now - lastPendingInputRetrievalTime;
            lastPendingInputRetrievalTime = now;

            inputs.Clear();

            bool reachedPreviousMouseSource = false;

            foreach (var h in InputHandlers)
            {
                if (!h.IsActive)
                    continue;

                dequeuedInputs.Clear();
                h.CollectPendingInputs(dequeuedInputs);

                foreach (var i in dequeuedInputs)
                {
                    // To avoid the same device reporting via two channels (and causing feedback), only one handler should be allowed to
                    // report mouse position data at a time. Handlers are given priority based on their constructed order.
                    // Devices which higher priority are allowed to take over control immediately, after which a delay is enforced (on every subsequent positional report)
                    // before a lower priority device can obtain control.
                    if (i is MousePositionAbsoluteInput || i is MousePositionRelativeInput)
                    {
                        if (mouseSource == null // a new device taking control when no existing preference is present.
                            || mouseSource == h // if this is the device which currently has control, renew the debounce delay.
                            || !reachedPreviousMouseSource // we have not reached the previous mouse source, so a higher priority device can take over control.
                            || mouseSourceDebounceTimeRemaining <= 0) // a lower priority device taking over control if the debounce delay has elapsed.
                        {
                            mouseSource = h;
                            mouseSourceDebounceTimeRemaining = mouse_source_debounce_time;
                        }
                        else
                        {
                            // drop positional input if we did not meet the criteria to be the current reporting handler.
                            continue;
                        }
                    }

                    inputs.Add(i);
                }

                // track whether we have passed the handler which is currently in control of positional handling.
                // importantly, this is updated regardless of whether the handler has reported any new inputs.
                if (mouseSource == h)
                    reachedPreviousMouseSource = true;
            }

            mouseSourceDebounceTimeRemaining -= elapsed;
            return inputs;
        }

        private readonly List<Drawable> inputQueue = new List<Drawable>();

        private SlimReadOnlyListWrapper<Drawable> buildNonPositionalInputQueue()
        {
            inputQueue.Clear();

            if (this is UserInputManager)
                FrameStatistics.Increment(StatisticsCounterType.InputQueue);

            var children = AliveInternalChildren;
            for (int i = 0; i < children.Count; i++)
                children[i].BuildNonPositionalInputQueue(inputQueue);

            if (!unfocusIfNoLongerValid())
            {
                inputQueue.Remove(FocusedDrawable);
                inputQueue.Add(FocusedDrawable);
            }

            // queues were created in back-to-front order.
            // We want input to first reach front-most drawables, so the queues
            // need to be reversed.
            inputQueue.Reverse();

            return inputQueue.AsSlimReadOnly();
        }

        private readonly List<Drawable> positionalInputQueue = new List<Drawable>();

        private SlimReadOnlyListWrapper<Drawable> buildPositionalInputQueue(Vector2 screenSpacePos)
        {
            positionalInputQueue.Clear();

            if (this is UserInputManager)
                FrameStatistics.Increment(StatisticsCounterType.PositionalIQ);

            var children = AliveInternalChildren;
            for (int i = 0; i < children.Count; i++)
                children[i].BuildPositionalInputQueue(screenSpacePos, positionalInputQueue);

            positionalInputQueue.Reverse();
            return positionalInputQueue.AsSlimReadOnly();
        }

        /// <summary>
        /// Whether this input manager is in a state it should handle hover events.
        /// This could for instance be set to false when the window/target does not have input focus.
        /// </summary>
        public virtual bool HandleHoverEvents => true;

        private void updateHoverEvents(InputState state)
        {
            Drawable lastHoverHandledDrawable = hoverHandledDrawable;
            hoverHandledDrawable = null;

            lastHoveredDrawables.Clear();
            lastHoveredDrawables.AddRange(hoveredDrawables);

            hoveredDrawables.Clear();

            // New drawables shouldn't be hovered if the cursor isn't in the window
            if (HandleHoverEvents)
            {
                // First, we need to construct hoveredDrawables for the current frame
                foreach (Drawable d in PositionalInputQueue)
                {
                    hoveredDrawables.Add(d);
                    lastHoveredDrawables.Remove(d);

                    // Don't need to re-hover those that are already hovered
                    if (d.IsHovered)
                    {
                        // Check if this drawable previously handled hover, and assume it would once more
                        if (d == lastHoverHandledDrawable)
                        {
                            hoverHandledDrawable = lastHoverHandledDrawable;
                            break;
                        }

                        continue;
                    }

                    d.IsHovered = true;

                    if (d.TriggerEvent(new HoverEvent(state)))
                    {
                        hoverHandledDrawable = d;
                        break;
                    }
                }
            }

            // Unhover all previously hovered drawables which are no longer hovered.
            foreach (Drawable d in lastHoveredDrawables)
            {
                d.IsHovered = false;
                d.TriggerEvent(new HoverLostEvent(state));
            }

            hoverEventsUpdated = true;
        }

        private bool isModifierKey(Key k) =>
            k == Key.LControl || k == Key.RControl
                              || k == Key.LAlt || k == Key.RAlt
                              || k == Key.LShift || k == Key.RShift
                              || k == Key.LWin || k == Key.RWin;

        protected virtual void HandleKeyboardKeyStateChange(ButtonStateChangeEvent<Key> keyboardKeyStateChange)
        {
            var state = keyboardKeyStateChange.State;
            var key = keyboardKeyStateChange.Button;
            var kind = keyboardKeyStateChange.Kind;

            GetButtonEventManagerFor(key).HandleButtonStateChange(state, kind);

            if (kind == ButtonStateChangeKind.Pressed)
            {
                if (!isModifierKey(key))
                {
                    keyboardRepeatKey = key;
                    keyboardRepeatTime = repeat_initial_delay;
                }
            }
            else
            {
                if (key == keyboardRepeatKey)
                {
                    keyboardRepeatKey = null;
                    keyboardRepeatTime = 0;
                }
            }
        }

        protected virtual void HandleTouchStateChange(TouchStateChangeEvent e)
        {
            Debug.Assert(e.LastPosition != null || e.IsActive != null, $"A {nameof(TouchStateChangeEvent)} provided with no changes information.");

            var manager = GetButtonEventManagerFor(e.Touch.Source);

            if (e.LastPosition is Vector2 lastPosition)
                manager.HandlePositionChange(e.State, lastPosition);

            if (e.IsActive is bool active)
                manager.HandleButtonStateChange(e.State, active ? ButtonStateChangeKind.Pressed : ButtonStateChangeKind.Released);
        }

        /// <summary>
        /// The number of touches which are currently active, causing a single cumulative "mouse down" state.
        /// </summary>
        private readonly HashSet<TouchSource> mouseMappedTouchesDown = new HashSet<TouchSource>();

        /// <summary>
        /// Handles latest activated touch state change event to produce mouse input from.
        /// </summary>
        /// <param name="e">The latest activated touch state change event.</param>
        /// <returns>Whether mouse input has been performed accordingly.</returns>
        protected virtual bool HandleMouseTouchStateChange(TouchStateChangeEvent e)
        {
            if (!MapMouseToLatestTouch)
                return false;

            if (e.IsActive == true || e.LastPosition != null)
            {
                new MousePositionAbsoluteInputFromTouch(e)
                {
                    Position = e.Touch.Position
                }.Apply(CurrentState, this);
            }

            switch (e.IsActive)
            {
                case true:
                    mouseMappedTouchesDown.Add(e.Touch.Source);
                    break;

                case false:
                    mouseMappedTouchesDown.Remove(e.Touch.Source);
                    break;
            }

            new MouseButtonInputFromTouch(MouseButton.Left, mouseMappedTouchesDown.Count > 0, e).Apply(CurrentState, this);
            return true;
        }

        protected virtual void HandleTabletPenButtonStateChange(ButtonStateChangeEvent<TabletPenButton> tabletPenButtonStateChange)
            => GetButtonEventManagerFor(tabletPenButtonStateChange.Button).HandleButtonStateChange(tabletPenButtonStateChange.State, tabletPenButtonStateChange.Kind);

        protected virtual void HandleTabletAuxiliaryButtonStateChange(ButtonStateChangeEvent<TabletAuxiliaryButton> tabletAuxiliaryButtonStateChange)
            => GetButtonEventManagerFor(tabletAuxiliaryButtonStateChange.Button).HandleButtonStateChange(tabletAuxiliaryButtonStateChange.State, tabletAuxiliaryButtonStateChange.Kind);

        protected virtual void HandleJoystickButtonStateChange(ButtonStateChangeEvent<JoystickButton> joystickButtonStateChange)
            => GetButtonEventManagerFor(joystickButtonStateChange.Button).HandleButtonStateChange(joystickButtonStateChange.State, joystickButtonStateChange.Kind);

        protected virtual void HandleMidiKeyStateChange(ButtonStateChangeEvent<MidiKey> midiKeyStateChange)
            => GetButtonEventManagerFor(midiKeyStateChange.Button).HandleButtonStateChange(midiKeyStateChange.State, midiKeyStateChange.Kind);

        public virtual void HandleInputStateChange(InputStateChangeEvent inputStateChange)
        {
            switch (inputStateChange)
            {
                case MousePositionChangeEvent mousePositionChange:
                    HandleMousePositionChange(mousePositionChange);
                    return;

                case MouseScrollChangeEvent mouseScrollChange:
                    HandleMouseScrollChange(mouseScrollChange);
                    return;

                case ButtonStateChangeEvent<MouseButton> mouseButtonStateChange:
                    HandleMouseButtonStateChange(mouseButtonStateChange);
                    return;

                case ButtonStateChangeEvent<Key> keyboardKeyStateChange:
                    HandleKeyboardKeyStateChange(keyboardKeyStateChange);
                    return;

                case TouchStateChangeEvent touchChange:
                    var manager = GetButtonEventManagerFor(touchChange.Touch.Source);

                    bool touchWasHandled = manager.HeldDrawable != null;

                    HandleTouchStateChange(touchChange);

                    bool touchIsHandled = manager.HeldDrawable != null;

                    // Produce mouse input if no drawable in the input queue has handled this touch event.
                    // Done for compatibility with components that do not handle touch input directly.
                    if (!touchWasHandled && !touchIsHandled)
                        HandleMouseTouchStateChange(touchChange);

                    return;

                case ButtonStateChangeEvent<TabletPenButton> tabletPenButtonStateChange:
                    HandleTabletPenButtonStateChange(tabletPenButtonStateChange);
                    return;

                case ButtonStateChangeEvent<TabletAuxiliaryButton> tabletAuxiliaryButtonStateChange:
                    HandleTabletAuxiliaryButtonStateChange(tabletAuxiliaryButtonStateChange);
                    return;

                case ButtonStateChangeEvent<JoystickButton> joystickButtonStateChange:
                    HandleJoystickButtonStateChange(joystickButtonStateChange);
                    return;

                case JoystickAxisChangeEvent joystickAxisChangeEvent:
                    HandleJoystickAxisChange(joystickAxisChangeEvent);
                    return;

                case ButtonStateChangeEvent<MidiKey> midiKeyStateChange:
                    HandleMidiKeyStateChange(midiKeyStateChange);
                    return;
            }
        }

        protected virtual void HandleJoystickAxisChange(JoystickAxisChangeEvent e)
            => GetJoystickAxisEventManagerFor(e.Axis.Source).HandleAxisChange(e.State, e.Axis.Value, e.LastValue);

        protected virtual void HandleMousePositionChange(MousePositionChangeEvent e)
        {
            var state = e.State;
            var mouse = state.Mouse;

            foreach (var h in InputHandlers)
            {
                if (h.Enabled.Value && h is INeedsMousePositionFeedback handler)
                    handler.FeedbackMousePositionChange(mouse.Position, h == mouseSource);
            }

            handleMouseMove(state, e.LastPosition);

            foreach (var manager in mouseButtonEventManagers.Values)
                manager.HandlePositionChange(state, e.LastPosition);

            updateHoverEvents(state);
        }

        protected virtual void HandleMouseScrollChange(MouseScrollChangeEvent e)
        {
            handleScroll(e.State, e.LastScroll, e.IsPrecise);
        }

        protected virtual void HandleMouseButtonStateChange(ButtonStateChangeEvent<MouseButton> e)
        {
            if (mouseButtonEventManagers.TryGetValue(e.Button, out var manager))
                manager.HandleButtonStateChange(e.State, e.Kind);
        }

        private bool handleMouseMove(InputState state, Vector2 lastPosition) => PropagateBlockableEvent(PositionalInputQueue, new MouseMoveEvent(state, lastPosition));

        private bool handleScroll(InputState state, Vector2 lastScroll, bool isPrecise) => PropagateBlockableEvent(PositionalInputQueue, new ScrollEvent(state, state.Mouse.Scroll - lastScroll, isPrecise));

        /// <summary>
        /// Triggers events on drawables in <paramref name="drawables"/> until it is handled.
        /// </summary>
        /// <param name="drawables">The drawables in the queue.</param>
        /// <param name="e">The event.</param>
        /// <returns>Whether the event was handled.</returns>
        protected virtual bool PropagateBlockableEvent(SlimReadOnlyListWrapper<Drawable> drawables, UIEvent e)
        {
            foreach (var d in drawables)
            {
                if (!d.TriggerEvent(e)) continue;

                if (shouldLog(e))
                {
                    string detail = d is ISuppressKeyEventLogging ? e.GetType().ReadableName() : e.ToString();
                    Logger.Log($"{detail} handled by {d}.", LoggingTarget.Runtime, LogLevel.Debug);
                }

                return true;
            }

            return false;
        }

        private bool shouldLog(UIEvent eventType)
        {
            switch (eventType)
            {
                case KeyDownEvent k:
                    return !k.Repeat;

                case DragEvent:
                case ScrollEvent:
                case MouseMoveEvent:
                    return false;

                default:
                    return true;
            }
        }

        /// <summary>
        /// Unfocus the current focused drawable if it is no longer in a valid state.
        /// </summary>
        /// <returns>true if there is no longer a focus.</returns>
        private bool unfocusIfNoLongerValid()
        {
            if (FocusedDrawable == null) return true;

            if (isDrawableValidForFocus(FocusedDrawable))
                return false;

            Logger.Log($"Focus on \"{FocusedDrawable}\" no longer valid as a result of {nameof(unfocusIfNoLongerValid)}.", LoggingTarget.Runtime, LogLevel.Debug);
            ChangeFocus(null);
            return true;
        }

        private bool isDrawableValidForFocus(Drawable drawable)
        {
            bool valid = drawable.IsAlive && drawable.IsPresent && drawable.Parent != null;

            if (valid)
            {
                //ensure we are visible
                CompositeDrawable d = drawable.Parent;

                while (d != null)
                {
                    if (!d.IsPresent || !d.IsAlive)
                    {
                        valid = false;
                        break;
                    }

                    d = d.Parent;
                }
            }

            return valid;
        }

        protected virtual void ChangeFocusFromClick(Drawable clickedDrawable)
        {
            Drawable focusTarget = null;

            if (clickedDrawable != null)
            {
                focusTarget = clickedDrawable;

                if (!focusTarget.AcceptsFocus)
                {
                    // search upwards from the clicked drawable until we find something to handle focus.
                    Drawable previousFocused = FocusedDrawable;

                    while (focusTarget?.AcceptsFocus == false)
                        focusTarget = focusTarget.Parent;

                    if (focusTarget != null && previousFocused != null)
                    {
                        // we found a focusable target above us.
                        // now search upwards from previousFocused to check whether focusTarget is a common parent.
                        Drawable search = previousFocused;
                        while (search != null && search != focusTarget)
                            search = search.Parent;

                        if (focusTarget == search)
                            // we have a common parent, so let's keep focus on the previously focused target.
                            focusTarget = previousFocused;
                    }
                }
            }

            ChangeFocus(focusTarget);
        }

        private void focusTopMostRequestingDrawable()
        {
            // todo: don't rebuild input queue every frame
            foreach (var d in NonPositionalInputQueue)
            {
                if (d.RequestsFocus)
                {
                    ChangeFocus(d);
                    return;
                }
            }

            ChangeFocus(null);
        }

        private class MouseLeftButtonEventManager : MouseButtonEventManager
        {
            public MouseLeftButtonEventManager(MouseButton button)
                : base(button)
            {
            }

            public override bool EnableDrag => true;

            public override bool EnableClick => true;

            public override bool ChangeFocusOnClick => true;
        }

        private class MouseMinorButtonEventManager : MouseButtonEventManager
        {
            public MouseMinorButtonEventManager(MouseButton button)
                : base(button)
            {
            }

            public override bool EnableDrag => false;

            public override bool EnableClick => false;

            public override bool ChangeFocusOnClick => false;
        }
    }
}
