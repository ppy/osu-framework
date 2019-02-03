// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Input.Events;
using osu.Framework.Input.Handlers;
using osu.Framework.Input.StateChanges;
using osu.Framework.Input.StateChanges.Events;
using osu.Framework.Input.States;
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
    public abstract class InputManager : Container, IInputStateChangeHandler
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

        protected abstract IEnumerable<InputHandler> InputHandlers { get; }

        private double keyboardRepeatTime;
        private Key? keyboardRepeatKey;

        /// <summary>
        /// The initial input state. <see cref="CurrentState"/> is always equal (as a reference) to the value returned from this.
        /// <see cref="InputState.Mouse"/>, <see cref="InputState.Keyboard"/> and <see cref="InputState.Joystick"/> should be non-null.
        /// </summary>
        protected virtual InputState CreateInitialState() => new InputState(new MouseState { IsPositionValid = false }, new KeyboardState(), new JoystickState());

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
        public IReadOnlyList<Drawable> HoveredDrawables => hoveredDrawables;

        /// <summary>
        /// Contains all <see cref="Drawable"/>s in top-down order which are considered
        /// for positional input. This list is the same as <see cref="HoveredDrawables"/>, only
        /// that the return value of <see cref="Drawable.OnHover"/> is not taken
        /// into account.
        /// </summary>
        public IEnumerable<Drawable> PositionalInputQueue => buildPositionalInputQueue(CurrentState);

        /// <summary>
        /// Contains all <see cref="Drawable"/>s in top-down order which are considered
        /// for non-positional input.
        /// </summary>
        public IEnumerable<Drawable> NonPositionalInputQueue => buildNonPositionalInputQueue();

        private readonly Dictionary<MouseButton, MouseButtonEventManager> mouseButtonEventManagers = new Dictionary<MouseButton, MouseButtonEventManager>();

        protected InputManager()
        {
            CurrentState = CreateInitialState();
            RelativeSizeAxes = Axes.Both;

            foreach (var button in Enum.GetValues(typeof(MouseButton)).Cast<MouseButton>())
            {
                var manager = CreateButtonManagerFor(button);
                manager.RequestFocus = ChangeFocusFromClick;
                manager.GetPositionalInputQueue = () => PositionalInputQueue;
                mouseButtonEventManagers.Add(button, manager);
            }
        }

        /// <summary>
        /// Create a <see cref="MouseButtonEventManager"/> for a specified mouse button.
        /// </summary>
        /// <param name="button">The button to be handled by the returned manager.</param>
        /// <returns>The <see cref="MouseButtonEventManager"/>.</returns>
        protected virtual MouseButtonEventManager CreateButtonManagerFor(MouseButton button)
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
        /// Changes the currently-focused drawable. First checks that <see cref="potentialFocusTarget"/> is in a valid state to receive focus,
        /// then unfocuses the current <see cref="FocusedDrawable"/> and focuses <see cref="potentialFocusTarget"/>.
        /// <see cref="potentialFocusTarget"/> can be null to reset focus.
        /// If the given drawable is already focused, nothing happens and no events are fired.
        /// </summary>
        /// <param name="potentialFocusTarget">The drawable to become focused.</param>
        /// <returns>True if the given drawable is now focused (or focus is dropped in the case of a null target).</returns>
        public bool ChangeFocus(Drawable potentialFocusTarget) => ChangeFocus(potentialFocusTarget, CurrentState);

        /// <summary>
        /// Changes the currently-focused drawable. First checks that <see cref="potentialFocusTarget"/> is in a valid state to receive focus,
        /// then unfocuses the current <see cref="FocusedDrawable"/> and focuses <see cref="potentialFocusTarget"/>.
        /// <see cref="potentialFocusTarget"/> can be null to reset focus.
        /// If the given drawable is already focused, nothing happens and no events are fired.
        /// </summary>
        /// <param name="potentialFocusTarget">The drawable to become focused.</param>
        /// <param name="state">The <see cref="InputState"/> associated with the focusing event.</param>
        /// <returns>True if the given drawable is now focused (or focus is dropped in the case of a null target).</returns>
        protected bool ChangeFocus(Drawable potentialFocusTarget, InputState state)
        {
            if (potentialFocusTarget == FocusedDrawable)
                return true;

            if (potentialFocusTarget != null && (!potentialFocusTarget.IsPresent || !potentialFocusTarget.AcceptsFocus))
                return false;

            var previousFocus = FocusedDrawable;

            FocusedDrawable = null;

            if (previousFocus != null)
            {
                previousFocus.HasFocus = false;
                previousFocus.TriggerEvent(new FocusLostEvent(state));

                if (FocusedDrawable != null) throw new InvalidOperationException($"Focus cannot be changed inside {nameof(OnFocusLost)}");
            }

            FocusedDrawable = potentialFocusTarget;

            Logger.Log($"Focus changed from {previousFocus?.ToString() ?? "nothing"} to {FocusedDrawable?.ToString() ?? "nothing"}.", LoggingTarget.Runtime, LogLevel.Debug);

            if (FocusedDrawable != null)
            {
                FocusedDrawable.HasFocus = true;
                FocusedDrawable.TriggerEvent(new FocusEvent(state));
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

        protected override void Update()
        {
            unfocusIfNoLongerValid();

            // aggressively clear to avoid holding references.
            inputQueue.Clear();
            positionalInputQueue.Clear();

            hoverEventsUpdated = false;

            foreach (var result in GetPendingInputs())
            {
                result.Apply(CurrentState, this);
            }

            if (CurrentState.Mouse.IsPositionValid)
            {
                PropagateBlockableEvent(PositionalInputQueue.Where(d => d is IRequireHighFrequencyMousePosition), new MouseMoveEvent(CurrentState));
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
                handleKeyDown(state, key, true);
                keyboardRepeatTime += repeat_tick_rate;
            }
        }

        protected virtual List<IInput> GetPendingInputs()
        {
            var inputs = new List<IInput>();

            foreach (var h in InputHandlers)
            {
                inputs.AddRange(h.GetPendingInputs());
            }

            return inputs;
        }

        private readonly List<Drawable> inputQueue = new List<Drawable>();

        private IEnumerable<Drawable> buildNonPositionalInputQueue()
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

            return inputQueue;
        }

        private readonly List<Drawable> positionalInputQueue = new List<Drawable>();

        private IEnumerable<Drawable> buildPositionalInputQueue(InputState state)
        {
            positionalInputQueue.Clear();

            if (this is UserInputManager)
                FrameStatistics.Increment(StatisticsCounterType.PositionalIQ);

            var children = AliveInternalChildren;
            for (int i = 0; i < children.Count; i++)
                children[i].BuildPositionalInputQueue(state.Mouse.Position, positionalInputQueue);

            positionalInputQueue.Reverse();
            return positionalInputQueue;
        }

        protected virtual bool HandleHoverEvents => true;

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

        private bool isModifierKey(Key k)
        {
            return k == Key.LControl || k == Key.RControl
                                     || k == Key.LAlt || k == Key.RAlt
                                     || k == Key.LShift || k == Key.RShift
                                     || k == Key.LWin || k == Key.RWin;
        }

        protected virtual void HandleKeyboardKeyStateChange(ButtonStateChangeEvent<Key> keyboardKeyStateChange)
        {
            var state = keyboardKeyStateChange.State;
            var key = keyboardKeyStateChange.Button;
            var kind = keyboardKeyStateChange.Kind;

            if (kind == ButtonStateChangeKind.Pressed)
            {
                handleKeyDown(state, key, false);

                if (!isModifierKey(key))
                {
                    keyboardRepeatKey = key;
                    keyboardRepeatTime = repeat_initial_delay;
                }
            }
            else
            {
                handleKeyUp(state, key);

                keyboardRepeatKey = null;
                keyboardRepeatTime = 0;
            }
        }

        protected virtual void HandleJoystickButtonStateChange(ButtonStateChangeEvent<JoystickButton> joystickButtonStateChange)
        {
            var state = joystickButtonStateChange.State;
            var button = joystickButtonStateChange.Button;
            var kind = joystickButtonStateChange.Kind;

            if (kind == ButtonStateChangeKind.Pressed)
            {
                handleJoystickPress(state, button);
            }
            else
            {
                handleJoystickRelease(state, button);
            }
        }

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
                case ButtonStateChangeEvent<JoystickButton> joystickButtonStateChange:
                    HandleJoystickButtonStateChange(joystickButtonStateChange);
                    return;
            }
        }

        protected virtual void HandleMousePositionChange(MousePositionChangeEvent e)
        {
            var state = e.State;
            var mouse = state.Mouse;

            foreach (var h in InputHandlers)
                if (h.Enabled && h is INeedsMousePositionFeedback handler)
                    handler.FeedbackMousePositionChange(mouse.Position);

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
                manager.HandleButtonStateChange(e.State, e.Kind, Time.Current);
        }

        private bool handleMouseMove(InputState state, Vector2 lastPosition)
        {
            return PropagateBlockableEvent(PositionalInputQueue, new MouseMoveEvent(state, lastPosition));
        }

        private bool handleScroll(InputState state, Vector2 lastScroll, bool isPrecise)
        {
            return PropagateBlockableEvent(PositionalInputQueue, new ScrollEvent(state, state.Mouse.Scroll - lastScroll, isPrecise));
        }

        private bool handleKeyDown(InputState state, Key key, bool repeat)
        {
            return PropagateBlockableEvent(NonPositionalInputQueue, new KeyDownEvent(state, key, repeat));
        }

        private bool handleKeyUp(InputState state, Key key)
        {
            return PropagateBlockableEvent(NonPositionalInputQueue, new KeyUpEvent(state, key));
        }

        private bool handleJoystickPress(InputState state, JoystickButton button)
        {
            return PropagateBlockableEvent(NonPositionalInputQueue, new JoystickPressEvent(state, button));
        }

        private bool handleJoystickRelease(InputState state, JoystickButton button)
        {
            return PropagateBlockableEvent(NonPositionalInputQueue, new JoystickReleaseEvent(state, button));
        }

        /// <summary>
        /// Triggers events on drawables in <paramref cref="drawables"/> until it is handled.
        /// </summary>
        /// <param name="drawables">The drawables in the queue.</param>
        /// <param name="e">The event.</param>
        /// <returns>Whether the event was handled.</returns>
        protected virtual bool PropagateBlockableEvent(IEnumerable<Drawable> drawables, UIEvent e)
        {
            var handledBy = drawables.FirstOrDefault(target => target.TriggerEvent(e));

            if (handledBy != null)
                Logger.Log($"{e} handled by {handledBy}.", LoggingTarget.Runtime, LogLevel.Debug);

            return handledBy != null;
        }

        /// <summary>
        /// Unfocus the current focused drawable if it is no longer in a valid state.
        /// </summary>
        /// <returns>true if there is no longer a focus.</returns>
        private bool unfocusIfNoLongerValid()
        {
            if (FocusedDrawable == null) return true;

            bool stillValid = FocusedDrawable.IsPresent && FocusedDrawable.Parent != null;

            if (stillValid)
            {
                //ensure we are visible
                CompositeDrawable d = FocusedDrawable.Parent;
                while (d != null)
                {
                    if (!d.IsPresent)
                    {
                        stillValid = false;
                        break;
                    }

                    d = d.Parent;
                }
            }

            if (stillValid)
                return false;

            Logger.Log($"Focus on \"{FocusedDrawable}\" no longer valid as a result of {nameof(unfocusIfNoLongerValid)}.", LoggingTarget.Runtime, LogLevel.Debug);
            ChangeFocus(null);
            return true;
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
            ChangeFocus(NonPositionalInputQueue.FirstOrDefault(target => target.RequestsFocus));
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
