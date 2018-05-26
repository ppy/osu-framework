// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Input.Handlers;
using osu.Framework.Logging;
using osu.Framework.Platform;
using OpenTK;
using OpenTK.Input;

namespace osu.Framework.Input
{
    public abstract class InputManager : Container
    {
        /// <summary>
        /// The initial delay before key repeat begins.
        /// </summary>
        private const int repeat_initial_delay = 250;

        /// <summary>
        /// The delay between key repeats after the initial repeat.
        /// </summary>
        private const int repeat_tick_rate = 70;

        /// <summary>
        /// The maximum time between two clicks for a double-click to be considered.
        /// </summary>
        private const int double_click_time = 250;

        /// <summary>
        /// The distance that must be moved until a dragged click becomes invalid.
        /// </summary>
        private const float click_drag_distance = 10;

        /// <summary>
        /// The time of the last input action.
        /// </summary>
        public double LastActionTime;

        protected GameHost Host;

        internal Drawable FocusedDrawable;

        protected abstract IEnumerable<InputHandler> InputHandlers { get; }

        private double lastClickTime;

        private double keyboardRepeatTime;

        private bool isDragging;

        private bool isValidClick;

        /// <summary>
        /// The last processed state.
        /// </summary>
        public InputState CurrentState = new InputState();

        /// <summary>
        /// The sequential list in which to handle mouse input.
        /// </summary>
        private readonly List<Drawable> positionalInputQueue = new List<Drawable>();

        /// <summary>
        /// The sequential list in which to handle keyboard input.
        /// </summary>
        private readonly List<Drawable> inputQueue = new List<Drawable>();

        /// <summary>
        /// The <see cref="Drawable"/> which is currently being dragged. null if none is.
        /// </summary>
        public Drawable DraggedDrawable { get; private set; }

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
        public IReadOnlyList<Drawable> PositionalInputQueue => positionalInputQueue;

        /// <summary>
        /// Contains all <see cref="Drawable"/>s in top-down order which are considered
        /// for non-positional input.
        /// </summary>
        public IReadOnlyList<Drawable> InputQueue => inputQueue;

        protected InputManager()
        {
            RelativeSizeAxes = Axes.Both;
        }

        [BackgroundDependencyLoader(permitNulls: true)]
        private void load(GameHost host)
        {
            Host = host;
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

            Logger.Log($"Focus switched to {FocusedDrawable?.ToString() ?? "nothing"}.", LoggingTarget.Runtime, LogLevel.Debug);

            if (FocusedDrawable != null)
            {
                FocusedDrawable.HasFocus = true;
                FocusedDrawable.TriggerOnFocus(state);
            }

            return true;
        }

        internal override bool BuildKeyboardInputQueue(List<Drawable> queue) => false;

        internal override bool BuildMouseInputQueue(Vector2 screenSpaceMousePos, List<Drawable> queue) => false;

        protected override void Update()
        {
            List<InputState> distinctStates = createDistinctStates(GetPendingStates()).ToList();

            //we need to make sure the code in the foreach below is run at least once even if we have no new pending states.
            if (distinctStates.Count == 0)
                distinctStates.Add(new InputState());

            unfocusIfNoLongerValid();

            foreach (InputState s in distinctStates)
                HandleNewState(s);

            if (CurrentState.Mouse != null)
            {
                foreach (var d in positionalInputQueue)
                    if (d is IRequireHighFrequencyMousePosition)
                        if (d.TriggerOnMouseMove(CurrentState))
                            break;
            }

            keyboardRepeatTime -= Time.Elapsed;

            if (FocusedDrawable == null)
                focusTopMostRequestingDrawable();

            base.Update();
        }

        protected virtual void HandleNewState(InputState state)
        {
            bool hasNewKeyboard = state.Keyboard != null;
            bool hasNewMouse = state.Mouse != null;
            bool hasNewJoystick = state.Joystick != null;

            var last = CurrentState;

            //avoid lingering references that would stay forever.
            last.Last = null;

            CurrentState = state;
            CurrentState.Last = last;

            if (CurrentState.Keyboard == null) CurrentState.Keyboard = last.Keyboard ?? new KeyboardState();
            if (CurrentState.Mouse == null) CurrentState.Mouse = last.Mouse ?? new MouseState();
            if (CurrentState.Joystick == null) CurrentState.Joystick = last.Joystick ?? new JoystickState();

            TransformState(CurrentState);

            //move above?
            updateInputQueues(CurrentState);

            //hover could change even when the mouse state has not.
            updateHoverEvents(CurrentState);

            if (hasNewMouse)
                updateMouseEvents(CurrentState);

            if (hasNewKeyboard || CurrentState.Keyboard.Keys.Any())
                updateKeyboardEvents(CurrentState);

            if (hasNewJoystick || CurrentState.Joystick.Buttons.Any())
                updateJoystickEvents(CurrentState);
        }

        protected virtual List<InputState> GetPendingStates()
        {
            var pendingStates = new List<InputState>();

            foreach (var h in InputHandlers)
            {
                if (h.IsActive && h.Enabled)
                    pendingStates.AddRange(h.GetPendingStates());
                else
                    h.GetPendingStates();
            }

            return pendingStates;
        }

        protected virtual void TransformState(InputState inputState)
        {
        }

        private void updateInputQueues(InputState state)
        {
            inputQueue.Clear();
            positionalInputQueue.Clear();

            if (state.Keyboard != null)
                foreach (Drawable d in AliveInternalChildren)
                    d.BuildKeyboardInputQueue(inputQueue);

            if (state.Mouse != null)
                foreach (Drawable d in AliveInternalChildren)
                    d.BuildMouseInputQueue(state.Mouse.Position, positionalInputQueue);

            // Keyboard and mouse queues were created in back-to-front order.
            // We want input to first reach front-most drawables, so the queues
            // need to be reversed.
            inputQueue.Reverse();
            positionalInputQueue.Reverse();
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
                foreach (Drawable d in positionalInputQueue)
                {
                    hoveredDrawables.Add(d);

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
            foreach (Drawable d in lastHoveredDrawables.Except(hoveredDrawables))
            {
                d.IsHovered = false;
                d.TriggerOnHoverLost(state);
            }
        }

        private void updateKeyboardEvents(InputState state)
        {
            KeyboardState keyboard = (KeyboardState)state.Keyboard;

            if (!keyboard.Keys.Any())
                keyboardRepeatTime = 0;

            var last = state.Last?.Keyboard;

            if (last == null) return;

            foreach (var k in last.Keys)
            {
                if (!keyboard.Keys.Contains(k))
                    handleKeyUp(state, k);
            }

            foreach (Key k in keyboard.Keys.Distinct())
            {
                bool isModifier = k == Key.LControl || k == Key.RControl
                                                    || k == Key.LAlt || k == Key.RAlt
                                                    || k == Key.LShift || k == Key.RShift
                                                    || k == Key.LWin || k == Key.RWin;

                LastActionTime = Time.Current;

                bool isRepetition = last.Keys.Contains(k);

                if (isModifier)
                {
                    //modifiers shouldn't affect or report key repeat
                    if (!isRepetition)
                        handleKeyDown(state, k, false);
                    continue;
                }

                if (isRepetition)
                {
                    if (keyboardRepeatTime <= 0)
                    {
                        keyboardRepeatTime += repeat_tick_rate;
                        handleKeyDown(state, k, true);
                    }
                }
                else
                {
                    keyboardRepeatTime = repeat_initial_delay;
                    handleKeyDown(state, k, false);
                }
            }
        }

        private void updateJoystickEvents(InputState state)
        {
            var joystick = (JoystickState)state.Joystick;

            var last = state.Last?.Joystick;
            if (last == null)
                return;

            foreach (var b in last.Buttons)
            {
                if (!joystick.Buttons.Contains(b))
                    handleJoystickRelease(state, b);
            }

            foreach (var b in joystick.Buttons)
            {
                if (!last.Buttons.Contains(b))
                {
                    LastActionTime = Time.Current;
                    handleJoystickPress(state, b);
                }
            }
        }

        private List<Drawable> mouseDownInputQueue;

        private void updateMouseEvents(InputState state)
        {
            MouseState mouse = (MouseState)state.Mouse;

            if (!(state.Last.Mouse is MouseState last)) return;

            mouse.LastPosition = last.Position;
            mouse.LastWheel = last.Wheel;

            if (mouse.Position != last.Position)
            {
                handleMouseMove(state);
                if (isDragging)
                    handleMouseDrag(state);
            }

            for (MouseButton b = 0; b < MouseButton.LastButton; b++)
            {
                var lastPressed = last.IsPressed(b);

                if (lastPressed != mouse.IsPressed(b))
                {
                    if (lastPressed)
                        handleMouseUp(state, b);
                    else
                        handleMouseDown(state, b);
                }
            }

            if (mouse.WheelDelta != 0 && (Host.Window?.CursorInWindow ?? true))
                handleWheel(state);

            if (mouse.HasAnyButtonPressed)
            {
                mouse.PositionMouseDown = last.PositionMouseDown;

                if (!last.HasAnyButtonPressed)
                {
                    //stuff which only happens once after the mousedown state
                    mouse.PositionMouseDown = mouse.Position;
                    LastActionTime = Time.Current;

                    if (mouse.IsPressed(MouseButton.Left))
                    {
                        isValidClick = true;

                        if (Time.Current - lastClickTime < double_click_time)
                        {
                            if (handleMouseDoubleClick(state))
                                //when we handle a double-click we want to block a normal click from firing.
                                isValidClick = false;

                            lastClickTime = 0;
                        }
                        else
                            lastClickTime = Time.Current;
                    }
                }

                if (!isDragging && Vector2Extensions.Distance(mouse.PositionMouseDown ?? mouse.Position, mouse.Position) > click_drag_distance)
                {
                    isDragging = true;
                    handleMouseDragStart(state);
                }
            }
            else if (last.HasAnyButtonPressed)
            {
                if (isValidClick && (DraggedDrawable == null || Vector2Extensions.Distance(last.PositionMouseDown ?? last.Position, mouse.Position) <= click_drag_distance))
                    handleMouseClick(state);

                mouseDownInputQueue = null;
                mouse.PositionMouseDown = null;
                isValidClick = false;

                if (isDragging)
                {
                    isDragging = false;
                    handleMouseDragEnd(state);
                }
            }
        }

        private bool handleMouseDown(InputState state, MouseButton button)
        {
            MouseDownEventArgs args = new MouseDownEventArgs
            {
                Button = button
            };

            var result = PropagateMouseDown(positionalInputQueue, state, args, out Drawable handledBy);

            mouseDownInputQueue = new List<Drawable>(result ? positionalInputQueue.Take(positionalInputQueue.IndexOf(handledBy) + 1) : positionalInputQueue);

            return result;
        }

        private bool handleMouseUp(InputState state, MouseButton button)
        {
            if (mouseDownInputQueue == null) return false;

            MouseUpEventArgs args = new MouseUpEventArgs
            {
                Button = button
            };

            //extra check for IsAlive because we are using an outdated queue.
            return PropagateMouseUp(mouseDownInputQueue.Where(target => target.IsAlive && target.IsPresent), state, args);
        }

        /// <summary>
        /// Triggers mouse up events on drawables in <paramref cref="drawables"/> until it is handled.
        /// </summary>
        /// <param name="drawables">The drawables in the queue.</param>
        /// <param name="state">The input state.</param>
        /// <param name="args">The args.</param>
        /// <returns>Whether the mouse up event was handled.</returns>
        protected virtual bool PropagateMouseUp(IEnumerable<Drawable> drawables, InputState state, MouseUpEventArgs args)
        {
            var handledBy = drawables.FirstOrDefault(target => target.TriggerOnMouseUp(state, args));

            if (handledBy != null)
                Logger.Log($"MouseUp ({args.Button}) handled by {handledBy}.", LoggingTarget.Runtime, LogLevel.Debug);

            return handledBy != null;
        }

        /// <summary>
        /// Triggers mouse down events on drawables in <paramref cref="drawables"/> until it is handled.
        /// </summary>
        /// <param name="drawables">The drawables in the queue.</param>
        /// <param name="state">The input state.</param>
        /// <param name="args">The args.</param>
        /// <param name="handledBy"></param>
        /// <returns>Whether the mouse down event was handled.</returns>
        protected virtual bool PropagateMouseDown(IEnumerable<Drawable> drawables, InputState state, MouseDownEventArgs args, out Drawable handledBy)
        {
            handledBy = drawables.FirstOrDefault(target => target.TriggerOnMouseDown(state, args));

            if (handledBy != null)
                Logger.Log($"MouseDown ({args.Button}) handled by {handledBy}.", LoggingTarget.Runtime, LogLevel.Debug);

            return handledBy != null;
        }

        private bool handleMouseMove(InputState state)
        {
            return positionalInputQueue.Any(target => target.TriggerOnMouseMove(state));
        }

        private Drawable clickedDrawable;

        private bool handleMouseClick(InputState state)
        {
            var intersectingQueue = positionalInputQueue.Intersect(mouseDownInputQueue);

            Drawable focusTarget = null;

            // click pass, triggering an OnClick on all drawables up to the first which returns true.
            // an extra IsHovered check is performed because we are using an outdated queue (for valid reasons which we need to document).
            clickedDrawable = intersectingQueue.FirstOrDefault(t => t.CanReceiveMouseInput && t.ReceiveMouseInputAt(state.Mouse.Position) && t.TriggerOnClick(state));

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

            ChangeFocus(focusTarget, state);

            if (clickedDrawable != null)
                Logger.Log($"MouseClick handled by {clickedDrawable}.", LoggingTarget.Runtime, LogLevel.Debug);

            return clickedDrawable != null;
        }

        private bool handleMouseDoubleClick(InputState state)
        {
            if (clickedDrawable == null) return false;

            return clickedDrawable.ReceiveMouseInputAt(state.Mouse.Position) && clickedDrawable.TriggerOnDoubleClick(state);
        }

        private bool handleMouseDrag(InputState state)
        {
            //Once a drawable is dragged, it remains in a dragged state until the drag is finished.
            return DraggedDrawable?.TriggerOnDrag(state) ?? false;
        }

        private bool handleMouseDragStart(InputState state)
        {
            Trace.Assert(DraggedDrawable == null, "The draggingDrawable was not set to null by handleMouseDragEnd.");
            DraggedDrawable = mouseDownInputQueue?.FirstOrDefault(target => target.IsAlive && target.IsPresent && target.TriggerOnDragStart(state));
            if (DraggedDrawable != null)
            {
                DraggedDrawable.IsDragged = true;
                Logger.Log($"MouseDragStart handled by {DraggedDrawable}.", LoggingTarget.Runtime, LogLevel.Debug);
            }

            return DraggedDrawable != null;
        }

        private bool handleMouseDragEnd(InputState state)
        {
            if (DraggedDrawable == null)
                return false;

            bool result = DraggedDrawable.TriggerOnDragEnd(state);
            DraggedDrawable.IsDragged = false;
            DraggedDrawable = null;

            return result;
        }

        private bool handleWheel(InputState state)
        {
            return PropagateWheel(positionalInputQueue, state);
        }

        /// <summary>
        /// Triggers wheel events on drawables in <paramref cref="drawables"/> until it is handled.
        /// </summary>
        /// <param name="drawables">The drawables in the queue.</param>
        /// <param name="state">The input state.</param>
        /// <returns></returns>
        protected virtual bool PropagateWheel(IEnumerable<Drawable> drawables, InputState state)
        {
            var handledBy = drawables.FirstOrDefault(target => target.TriggerOnWheel(state));

            if (handledBy != null)
                Logger.Log($"Wheel ({state.Mouse.WheelDelta}) handled by {handledBy}.", LoggingTarget.Runtime, LogLevel.Debug);

            return handledBy != null;
        }

        private bool handleKeyDown(InputState state, Key key, bool repeat)
        {
            IEnumerable<Drawable> queue = inputQueue;
            if (!unfocusIfNoLongerValid())
                queue = queue.Prepend(FocusedDrawable);

            return PropagateKeyDown(queue, state, new KeyDownEventArgs { Key = key, Repeat = repeat });
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
            IEnumerable<Drawable> queue = inputQueue;
            if (!unfocusIfNoLongerValid())
                queue = queue.Prepend(FocusedDrawable);

            return PropagateKeyUp(queue, state, new KeyUpEventArgs { Key = key });
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
            IEnumerable<Drawable> queue = inputQueue;
            if (!unfocusIfNoLongerValid())
                queue = new[] { FocusedDrawable }.Concat(queue);

            return PropagateJoystickPress(queue, state, new JoystickEventArgs { Button = button });
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
            IEnumerable<Drawable> queue = inputQueue;
            if (!unfocusIfNoLongerValid())
                queue = new[] { FocusedDrawable }.Concat(queue);

            return PropagateJoystickRelease(queue, state, new JoystickEventArgs { Button = button });
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

            ChangeFocus(null);
            return true;
        }

        private void focusTopMostRequestingDrawable() => ChangeFocus(inputQueue.FirstOrDefault(target => target.RequestsFocus));

        /// <summary>
        /// In order to provide a reliable event system to drawables, we want to ensure that we reprocess input queues (via the
        /// main loop in <see cref="InputManager"/> after each and every button or key change. This allows
        /// correct behaviour in a case where the input queues change based on triggered by a button or key.
        /// </summary>
        /// <param name="states">One ore more states which are to be converted to distinct states.</param>
        /// <returns>Processed states such that at most one attribute change occurs between any two consecutive states.</returns>
        private IEnumerable<InputState> createDistinctStates(IEnumerable<InputState> states)
        {
            InputState transientState = CurrentState;
            IKeyboardState lastKeyboard = CurrentState.Keyboard ?? new KeyboardState();
            IMouseState lastMouse = CurrentState.Mouse ?? new MouseState();
            IJoystickState lastJoystick = CurrentState.Joystick ?? new JoystickState();

            InputState createDistinctState(Action<InputState> application)
            {
                var lastState = transientState;
                lastState.Last = null;

                transientState = lastState.Clone();
                transientState.Last = lastState;

                application?.Invoke(transientState);

                lastKeyboard = transientState.Keyboard ?? lastKeyboard;
                lastMouse = transientState.Mouse ?? lastMouse;
                lastJoystick = transientState.Joystick ?? lastJoystick;

                return transientState;
            }

            foreach (var incoming in states)
            {
                if (incoming.Mouse != null)
                {
                    if (lastMouse.Position != incoming.Mouse.Position)
                        yield return createDistinctState(s =>
                        {
                            s.Mouse = (s.Mouse as MouseState)?.CloneWithoutDeltas() ?? new MouseState();
                            s.Mouse.Position = incoming.Mouse.Position;
                        });

                    if (lastMouse.Wheel != incoming.Mouse.Wheel)
                        yield return createDistinctState(s =>
                        {
                            s.Mouse = s.Mouse.Clone();
                            s.Mouse.Wheel = incoming.Mouse.Wheel;
                        });

                    foreach (var releasedButton in lastMouse.Buttons.Except(incoming.Mouse.Buttons))
                        yield return createDistinctState(s =>
                        {
                            s.Mouse = s.Mouse.Clone();
                            s.Mouse.Buttons = s.Mouse.Buttons.Where(d => d != releasedButton).ToArray();
                        });

                    foreach (var pressedButton in incoming.Mouse.Buttons.Except(lastMouse.Buttons))
                        yield return createDistinctState(s =>
                        {
                            s.Mouse = s.Mouse.Clone();
                            s.Mouse.Buttons = s.Mouse.Buttons.Union(new[] { pressedButton }).ToArray();
                        });
                }

                if (incoming.Keyboard != null)
                {
                    foreach (var releasedKey in lastKeyboard.Keys.Except(incoming.Keyboard.Keys))
                        yield return createDistinctState(s => s.Keyboard = new KeyboardState { Keys = s.Keyboard.Keys.Where(d => d != releasedKey).ToArray() });

                    foreach (var pressedKey in incoming.Keyboard.Keys.Except(lastKeyboard.Keys))
                        yield return createDistinctState(s => s.Keyboard = new KeyboardState { Keys = s.Keyboard.Keys.Union(new[] { pressedKey }) });
                }

                if (incoming.Joystick != null)
                {
                    // push a state for the axes
                    yield return createDistinctState(s => s.Joystick = new JoystickState
                    {
                        Axes = incoming.Joystick.Axes,
                        Buttons = s.Joystick.Buttons
                    });

                    foreach (var releasedButton in lastJoystick.Buttons.Except(incoming.Joystick.Buttons))
                        yield return createDistinctState(s => s.Joystick = new JoystickState
                        {
                            Axes = s.Joystick.Axes,
                            Buttons = s.Joystick.Buttons.Where(d => d != releasedButton).ToArray()
                        });

                    foreach (var pressedButton in incoming.Joystick.Buttons.Except(lastJoystick.Buttons))
                        yield return createDistinctState(s => s.Joystick = new JoystickState
                        {
                            Axes = s.Joystick.Axes,
                            Buttons = s.Joystick.Buttons.Union(new[] { pressedButton }).ToArray()
                        });
                }

                yield return incoming;
            }
        }
    }

    public enum ConfineMouseMode
    {
        Never,
        Fullscreen,
        Always
    }
}
