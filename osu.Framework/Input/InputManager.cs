// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Input.EventArgs;
using osu.Framework.Input.Handlers;
using osu.Framework.Input.StateChanges;
using osu.Framework.Input.States;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osu.Framework.Statistics;
using OpenTK;
using OpenTK.Input;
using JoystickEventArgs = osu.Framework.Input.EventArgs.JoystickEventArgs;
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

        internal Drawable FocusedDrawable;

        protected abstract IEnumerable<InputHandler> InputHandlers { get; }

        private double keyboardRepeatTime;
        private Key? keyboardRepeatKey;

        /// <summary>
        /// The initial input state. <see cref="CurrentState"/> is always equal (as a reference) to the value returned from this.
        /// <see cref="InputState.Mouse"/>, <see cref="InputState.Keyboard"/> and <see cref="InputState.Joystick"/> should be non-null.
        /// </summary>
        protected virtual InputState CreateInitialState() => new InputState
        {
            Mouse = new MouseState { IsPositionValid = false },
            Keyboard = new KeyboardState(),
            Joystick = new JoystickState(),
        };

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
        /// which returned true in its <see cref="Drawable.OnHover(InputState)"/> method.
        /// Top-down in this case means reverse draw order, i.e. the front-most visible
        /// <see cref="Drawable"/> first, and <see cref="Container"/>s after their children.
        /// </summary>
        private readonly List<Drawable> hoveredDrawables = new List<Drawable>();

        /// <summary>
        /// The <see cref="Drawable"/> which returned true in its
        /// <see cref="Drawable.OnHover(InputState)"/> method, or null if none did so.
        /// </summary>
        private Drawable hoverHandledDrawable;

        /// <summary>
        /// Contains all hovered <see cref="Drawable"/>s in top-down order up to the first
        /// which returned true in its <see cref="Drawable.OnHover(InputState)"/> method.
        /// Top-down in this case means reverse draw order, i.e. the front-most visible
        /// <see cref="Drawable"/> first, and <see cref="Container"/>s after their children.
        /// </summary>
        public IReadOnlyList<Drawable> HoveredDrawables => hoveredDrawables;

        /// <summary>
        /// Contains all <see cref="Drawable"/>s in top-down order which are considered
        /// for positional input. This list is the same as <see cref="HoveredDrawables"/>, only
        /// that the return value of <see cref="Drawable.OnHover(InputState)"/> is not taken
        /// into account.
        /// </summary>
        public IEnumerable<Drawable> PositionalInputQueue => buildMouseInputQueue(CurrentState);

        /// <summary>
        /// Contains all <see cref="Drawable"/>s in top-down order which are considered
        /// for non-positional input.
        /// </summary>
        public IEnumerable<Drawable> InputQueue => buildInputQueue();

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
                previousFocus.TriggerOnFocusLost(state);

                if (FocusedDrawable != null) throw new InvalidOperationException($"Focus cannot be changed inside {nameof(OnFocusLost)}");
            }

            FocusedDrawable = potentialFocusTarget;

            Logger.Log($"Focus changed from {previousFocus?.ToString() ?? "nothing"} to {FocusedDrawable?.ToString() ?? "nothing"}.", LoggingTarget.Runtime, LogLevel.Debug);

            if (FocusedDrawable != null)
            {
                FocusedDrawable.HasFocus = true;
                FocusedDrawable.TriggerOnFocus(state);
            }

            return true;
        }

        internal override bool BuildKeyboardInputQueue(List<Drawable> queue, bool allowBlocking = true)
        {
            if (!allowBlocking)
                base.BuildKeyboardInputQueue(queue, false);

            return false;
        }

        internal override bool BuildMouseInputQueue(Vector2 screenSpaceMousePos, List<Drawable> queue) => false;

        protected override void Update()
        {
            unfocusIfNoLongerValid();

            // aggressively clear to avoid holding references.
            inputQueue.Clear();
            positionalInputQueue.Clear();

            foreach (var result in GetPendingInputs())
            {
                result.Apply(CurrentState, this);
            }

            if (CurrentState.Mouse.IsPositionValid)
            {
                foreach (var d in PositionalInputQueue)
                    if (d is IRequireHighFrequencyMousePosition)
                        if (d.TriggerOnMouseMove(CurrentState))
                            break;
            }

            updateKeyRepeat(CurrentState);

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

        private IEnumerable<Drawable> buildInputQueue()
        {
            inputQueue.Clear();

            if (this is UserInputManager)
                FrameStatistics.Increment(StatisticsCounterType.KeyboardQueue);

            var children = AliveInternalChildren;
            for (int i = 0; i < children.Count; i++)
                children[i].BuildKeyboardInputQueue(inputQueue);

            if (!unfocusIfNoLongerValid())
            {
                inputQueue.Remove(FocusedDrawable);
                inputQueue.Add(FocusedDrawable);
            }

            // Keyboard and mouse queues were created in back-to-front order.
            // We want input to first reach front-most drawables, so the queues
            // need to be reversed.
            inputQueue.Reverse();

            return inputQueue;
        }

        private readonly List<Drawable> positionalInputQueue = new List<Drawable>();

        private IEnumerable<Drawable> buildMouseInputQueue(InputState state)
        {
            positionalInputQueue.Clear();

            if (this is UserInputManager)
                FrameStatistics.Increment(StatisticsCounterType.MouseQueue);

            var children = AliveInternalChildren;
            for (int i = 0; i < children.Count; i++)
                children[i].BuildMouseInputQueue(state.Mouse.Position, positionalInputQueue);

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
                    if (d.TriggerOnHover(state))
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
                d.TriggerOnHoverLost(state);
            }
        }

        private bool isModifierKey(Key k)
        {
            return k == Key.LControl || k == Key.RControl
                                     || k == Key.LAlt || k == Key.RAlt
                                     || k == Key.LShift || k == Key.RShift
                                     || k == Key.LWin || k == Key.RWin;
        }

        public virtual void HandleKeyboardKeyStateChange(InputState state, Key key, ButtonStateChangeKind kind)
        {
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

        public virtual void HandleJoystickButtonStateChange(InputState state, JoystickButton button, ButtonStateChangeKind kind)
        {
            if (kind == ButtonStateChangeKind.Pressed)
            {
                handleJoystickPress(state, button);
            }
            else
            {
                handleJoystickRelease(state, button);
            }
        }

        public virtual void HandleMousePositionChange(InputState state)
        {
            var mouse = state.Mouse;

            foreach (var h in InputHandlers)
                if (h.Enabled && h is INeedsMousePositionFeedback handler)
                    handler.FeedbackMousePositionChange(mouse.Position);

            handleMouseMove(state);

            foreach (var manager in mouseButtonEventManagers.Values)
                manager.HandlePositionChange(state);
        }

        public virtual void HandleMouseScrollChange(InputState state)
        {
            handleScroll(state);
        }

        public void HandleMouseButtonStateChange(InputState state, MouseButton button, ButtonStateChangeKind kind)
        {
            if (mouseButtonEventManagers.TryGetValue(button, out var manager))
                manager.HandleButtonStateChange(state, kind, Time.Current);
        }

        public virtual void HandleCustomInput(InputState state, IInput input)
        {
        }

        private bool handleMouseMove(InputState state)
        {
            return PositionalInputQueue.Any(target => target.TriggerOnMouseMove(state));
        }

        private bool handleScroll(InputState state)
        {
            return PropagateScroll(PositionalInputQueue, state);
        }

        /// <summary>
        /// Triggers scroll events on drawables in <paramref cref="drawables"/> until it is handled.
        /// </summary>
        /// <param name="drawables">The drawables in the queue.</param>
        /// <param name="state">The input state.</param>
        /// <returns></returns>
        protected virtual bool PropagateScroll(IEnumerable<Drawable> drawables, InputState state)
        {
            var handledBy = drawables.FirstOrDefault(target => target.TriggerOnScroll(state));

            if (handledBy != null)
                Logger.Log($"Scroll ({state.Mouse.ScrollDelta.X:#,2},{state.Mouse.ScrollDelta.Y:#,2}) handled by {handledBy}.", LoggingTarget.Runtime, LogLevel.Debug);

            return handledBy != null;
        }

        private bool handleKeyDown(InputState state, Key key, bool repeat)
        {
            return PropagateKeyDown(InputQueue, state, new KeyDownEventArgs { Key = key, Repeat = repeat });
        }

        /// <summary>
        /// Triggers key down events on drawables in <paramref cref="drawables"/> until it is handled.
        /// </summary>
        /// <param name="drawables">The drawables in the queue.</param>
        /// <param name="state">The input state.</param>
        /// <param name="args">The args.</param>
        /// <returns>Whether the key down event was handled.</returns>
        protected virtual bool PropagateKeyDown(IEnumerable<Drawable> drawables, InputState state, KeyDownEventArgs args)
        {
            var handledBy = drawables.FirstOrDefault(target => target.TriggerOnKeyDown(state, args));

            if (handledBy != null)
                Logger.Log($"KeyDown ({args.Key}) handled by {handledBy}.", LoggingTarget.Runtime, LogLevel.Debug);

            return handledBy != null;
        }

        private bool handleKeyUp(InputState state, Key key)
        {
            return PropagateKeyUp(InputQueue, state, new KeyUpEventArgs { Key = key });
        }

        /// <summary>
        /// Triggers key up events on drawables in <paramref cref="drawables"/> until it is handled.
        /// </summary>
        /// <param name="drawables">The drawables in the queue.</param>
        /// <param name="state">The input state.</param>
        /// <param name="args">The args.</param>
        /// <returns>Whether the key up event was handled.</returns>
        protected virtual bool PropagateKeyUp(IEnumerable<Drawable> drawables, InputState state, KeyUpEventArgs args)
        {
            var handledBy = drawables.FirstOrDefault(target => target.TriggerOnKeyUp(state, args));

            if (handledBy != null)
                Logger.Log($"KeyUp ({args.Key}) handled by {handledBy}.", LoggingTarget.Runtime, LogLevel.Debug);

            return handledBy != null;
        }

        private bool handleJoystickPress(InputState state, JoystickButton button)
        {
            return PropagateJoystickPress(InputQueue, state, new JoystickEventArgs { Button = button });
        }

        protected virtual bool PropagateJoystickPress(IEnumerable<Drawable> drawables, InputState state, JoystickEventArgs args)
        {
            var handledBy = drawables.FirstOrDefault(target => target.TriggerOnJoystickPress(state, args));

            if (handledBy != null)
                Logger.Log($"JoystickPress ({args.Button}) handled by {handledBy}.", LoggingTarget.Runtime, LogLevel.Debug);

            return handledBy != null;
        }

        private bool handleJoystickRelease(InputState state, JoystickButton button)
        {
            return PropagateJoystickRelease(InputQueue, state, new JoystickEventArgs { Button = button });
        }

        protected virtual bool PropagateJoystickRelease(IEnumerable<Drawable> drawables, InputState state, JoystickEventArgs args)
        {
            var handledBy = drawables.FirstOrDefault(target => target.TriggerOnJoystickRelease(state, args));

            if (handledBy != null)
                Logger.Log($"JoystickRelease ({args.Button}) handled by {handledBy}.", LoggingTarget.Runtime, LogLevel.Debug);

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
            ChangeFocus(InputQueue.FirstOrDefault(target => target.RequestsFocus));
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
