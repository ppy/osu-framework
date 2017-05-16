// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Input.Handlers;
using OpenTK;
using OpenTK.Input;
using osu.Framework.Platform;

namespace osu.Framework.Input
{
    public class InputManager : Container, IRequireHighFrequencyMousePosition
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
        /// The distance that must be moved before a drag begins.
        /// </summary>
        private const float drag_start_distance = 0;

        /// <summary>
        /// The distance that must be moved until a dragged click becomes invalid.
        /// </summary>
        private const float click_drag_distance = 40;

        /// <summary>
        /// The time of the last input action.
        /// </summary>
        public double LastActionTime;

        protected GameHost Host;

        internal Drawable FocusedDrawable;

        private readonly List<InputHandler> inputHandlers = new List<InputHandler>();

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
        private readonly List<Drawable> mouseInputQueue = new List<Drawable>();

        /// <summary>
        /// The sequential list in which to handle keyboard input.
        /// </summary>
        private readonly List<Drawable> keyboardInputQueue = new List<Drawable>();

        private Drawable draggingDrawable;
        private readonly List<Drawable> hoveredDrawables = new List<Drawable>();
        private Drawable hoverHandledDrawable;

        public IEnumerable<Drawable> HoveredDrawables => hoveredDrawables;

        public InputManager()
        {
            RelativeSizeAxes = Axes.Both;
        }

        [BackgroundDependencyLoader(permitNulls: true)]
        private void load(GameHost host)
        {
            Host = host;
        }

        /// <summary>
        /// Handles the internal passing on focus. Note that this doesn't perform a check on the new focus drawable.
        /// Usually you'd want to call TriggerFocus on the drawable directly instead.
        /// </summary>
        /// <param name="focus">The drawable to become focused.</param>
        internal void ChangeFocus(Drawable focus)
        {
            if (focus == FocusedDrawable) return;

            FocusedDrawable?.TriggerFocusLost(null, true);
            FocusedDrawable = focus;
            FocusedDrawable?.TriggerFocus(CurrentState, true);
        }

        internal override bool BuildKeyboardInputQueue(List<Drawable> queue) => false;

        internal override bool BuildMouseInputQueue(Vector2 screenSpaceMousePos, List<Drawable> queue) => false;

        protected override void Update()
        {
            var pendingStates = createDistinctInputStates(GetPendingStates()).ToArray();

            unfocusIfNoLongerValid(CurrentState);

            //we need to make sure the code in the foreach below is run at least once even if we have no new pending states.
            if (pendingStates.Length == 0)
                pendingStates = new[] { new InputState() };

            foreach (InputState s in pendingStates)
            {
                bool hasNewKeyboard = s.Keyboard != null;
                bool hasNewMouse = s.Mouse != null;

                var last = CurrentState;

                //avoid lingering references that would stay forever.
                last.Last = null;

                CurrentState = s;
                CurrentState.Last = last;

                if (CurrentState.Keyboard == null) CurrentState.Keyboard = last.Keyboard ?? new KeyboardState();
                if (CurrentState.Mouse == null) CurrentState.Mouse = last.Mouse ?? new MouseState();

                TransformState(CurrentState);

                //move above?
                updateInputQueues(CurrentState);

                (s.Mouse as MouseState)?.SetLast(last.Mouse); //necessary for now as last state is used internally for stuff

                //hover could change even when the mouse state has not.
                updateHoverEvents(CurrentState);

                if (hasNewMouse)
                    updateMouseEvents(CurrentState);

                if (hasNewKeyboard || CurrentState.Keyboard.Keys.Any())
                    updateKeyboardEvents(CurrentState);
            }

            if (CurrentState.Mouse != null)
            {
                foreach (var d in mouseInputQueue)
                    if (d is IRequireHighFrequencyMousePosition)
                        if (d.TriggerMouseMove(CurrentState)) break;
            }

            keyboardRepeatTime -= Time.Elapsed;

            if (FocusedDrawable == null)
                focusTopMostRequestingDrawable(CurrentState);

            base.Update();
        }

        /// <summary>
        /// In order to provide a reliable event system to drawables, we want to ensure that we reprocess input queues (via the
        /// main loop in<see cref="updateInputQueues(InputState)"/> after each and every button or key change. This allows
        /// correct behaviour in a case where the input queues change based on triggered by a button or key.
        /// </summary>
        /// <param name="states">A list of <see cref="InputState"/>s</param>
        /// <returns>Processed states such that at most one button change occurs between any two consecutive states.</returns>
        private IEnumerable<InputState> createDistinctInputStates(List<InputState> states)
        {
            InputState last = CurrentState;

            foreach (var i in states)
            {
                //first we want to create a copy of ourselves without any button changes
                //we do this by updating our buttons to the state of the last frame.
                var iWithoutButtons = i.Clone();

                var iHasMouse = iWithoutButtons.Mouse != null;
                var iHasKeyboard = iWithoutButtons.Keyboard != null;

                if (iHasMouse)
                    for (MouseButton b = 0; b < MouseButton.LastButton; b++)
                        iWithoutButtons.Mouse.SetPressed(b, last.Mouse?.IsPressed(b) ?? false);

                if (iHasKeyboard)
                    iWithoutButtons.Keyboard.Keys = last.Keyboard?.Keys ?? new Key[] { };

                //we start by adding this state to the processed list...
                yield return iWithoutButtons;
                last = iWithoutButtons;

                //and then iterate over each button/key change, adding intermediate states along the way.
                if (iHasMouse)
                {
                    for (MouseButton b = 0; b < MouseButton.LastButton; b++)
                    {
                        if (i.Mouse.IsPressed(b) != (last.Mouse?.IsPressed(b) ?? false))
                        {
                            var intermediateState = last.Clone();
                            if (intermediateState.Mouse == null) intermediateState.Mouse = new MouseState();

                            //add our single local change
                            intermediateState.Mouse.SetPressed(b, i.Mouse.IsPressed(b));

                            last = intermediateState;
                            yield return intermediateState;
                        }
                    }
                }

                if (iHasKeyboard)
                {
                    foreach (var releasedKey in last.Keyboard?.Keys.Except(i.Keyboard.Keys) ?? new Key[] { })
                    {
                        var intermediateState = last.Clone();
                        if (intermediateState.Keyboard == null) intermediateState.Keyboard = new KeyboardState();

                        intermediateState.Keyboard.Keys = intermediateState.Keyboard.Keys.Where(d => d != releasedKey);

                        last = intermediateState;
                        yield return intermediateState;
                    }

                    foreach (var pressedKey in i.Keyboard.Keys.Except(last.Keyboard?.Keys ?? new Key[] { }))
                    {
                        var intermediateState = last.Clone();
                        if (intermediateState.Keyboard == null) intermediateState.Keyboard = new KeyboardState();

                        intermediateState.Keyboard.Keys = intermediateState.Keyboard.Keys.Union(new[] { pressedKey });

                        last = intermediateState;
                        yield return intermediateState;
                    }
                }
            }
        }

        protected virtual List<InputState> GetPendingStates()
        {
            var pendingStates = new List<InputState>();

            foreach (var h in inputHandlers)
            {
                if (h.IsActive)
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
            keyboardInputQueue.Clear();
            mouseInputQueue.Clear();

            if (state.Keyboard != null)
                foreach (Drawable d in AliveInternalChildren)
                    d.BuildKeyboardInputQueue(keyboardInputQueue);

            if (state.Mouse != null)
                foreach (Drawable d in AliveInternalChildren)
                    d.BuildMouseInputQueue(state.Mouse.Position, mouseInputQueue);

            keyboardInputQueue.Reverse();
            mouseInputQueue.Reverse();
        }

        private void updateHoverEvents(InputState state)
        {
            Drawable lastHoverHandledDrawable = hoverHandledDrawable;
            hoverHandledDrawable = null;

            List<Drawable> lastHoveredDrawables = new List<Drawable>(hoveredDrawables);
            hoveredDrawables.Clear();

            // Unconditionally unhover all that aren't directly hovered anymore
            List<Drawable> newlyUnhoveredDrawables = lastHoveredDrawables.Except(mouseInputQueue).ToList();
            foreach (Drawable d in newlyUnhoveredDrawables)
            {
                d.Hovering = false;
                d.TriggerHoverLost(state);
            }

            // Don't care about what's now explicitly unhovered
            lastHoveredDrawables = lastHoveredDrawables.Except(newlyUnhoveredDrawables).ToList();

            // lastHoveredDrawables now contain only drawables that were hovered in the previous frame
            // that may continue being hovered. We need to construct hoveredDrawables for the current frame
            foreach (Drawable d in mouseInputQueue)
            {
                hoveredDrawables.Add(d);
                lastHoveredDrawables.Remove(d);

                // Don't need to re-hover those that are already hovered
                if (d.Hovering)
                {
                    // Check if this drawable previously handled hover, and assume it would once more
                    if (d == lastHoverHandledDrawable)
                    {
                        hoverHandledDrawable = lastHoverHandledDrawable;
                        break;
                    }

                    continue;
                }

                d.Hovering = true;
                if (d.TriggerHover(state))
                {
                    hoverHandledDrawable = d;
                    break;
                }
            }

            // lastHoveredDrawables now contains only drawables that were hovered in the previous frame
            // but should no longer be hovered as a result of a drawable handling hover this frame
            foreach (Drawable d in lastHoveredDrawables)
            {
                d.Hovering = false;
                d.TriggerHoverLost(state);
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

        private List<Drawable> mouseDownInputQueue;

        private void updateMouseEvents(InputState state)
        {
            MouseState mouse = (MouseState)state.Mouse;

            var last = state.Last?.Mouse as MouseState;

            if (last == null) return;

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

            if (mouse.WheelDelta != 0)
                handleWheel(state);

            if (mouse.HasAnyButtonPressed)
            {
                if (!last.HasAnyButtonPressed)
                {
                    //stuff which only happens once after the mousedown state
                    mouse.PositionMouseDown = state.Mouse.Position;
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

                        lastClickTime = Time.Current;
                    }
                }

                if (!isDragging && Vector2.Distance(mouse.PositionMouseDown ?? mouse.Position, mouse.Position) > drag_start_distance)
                {
                    isDragging = true;
                    handleMouseDragStart(state);
                }
            }
            else if (last.HasAnyButtonPressed)
            {
                if (isValidClick && (draggingDrawable == null || Vector2.Distance(mouse.PositionMouseDown ?? mouse.Position, mouse.Position) < click_drag_distance))
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

            mouseDownInputQueue = new List<Drawable>(mouseInputQueue);

            return mouseInputQueue.Find(target => target.TriggerMouseDown(state, args)) != null;
        }

        private bool handleMouseUp(InputState state, MouseButton button)
        {
            if (mouseDownInputQueue == null) return false;

            MouseUpEventArgs args = new MouseUpEventArgs
            {
                Button = button
            };

            //extra check for IsAlive because we are using an outdated queue.
            return mouseDownInputQueue.Any(target => target.IsAlive && target.IsPresent && target.TriggerMouseUp(state, args));
        }

        private bool handleMouseMove(InputState state)
        {
            return mouseInputQueue.Any(target => target.TriggerMouseMove(state));
        }

        private bool handleMouseClick(InputState state)
        {
            //extra check for IsAlive because we are using an outdated queue.
            if (mouseInputQueue.Intersect(mouseDownInputQueue).Any(t => t.IsHovered(state.Mouse.Position) && t.TriggerClick(state) | t.TriggerFocus(state, true)))
                return true;

            FocusedDrawable?.TriggerFocusLost();
            return false;
        }

        private bool handleMouseDoubleClick(InputState state)
        {
            return mouseInputQueue.Any(target => target.TriggerDoubleClick(state));
        }

        private bool handleMouseDrag(InputState state)
        {
            //Once a drawable is dragged, it remains in a dragged state until the drag is finished.
            return draggingDrawable?.TriggerDrag(state) ?? false;
        }

        private bool handleMouseDragStart(InputState state)
        {
            draggingDrawable = mouseDownInputQueue?.FirstOrDefault(target => target.IsAlive && target.TriggerDragStart(state));
            return draggingDrawable != null;
        }

        private bool handleMouseDragEnd(InputState state)
        {
            if (draggingDrawable == null)
                return false;

            bool result = draggingDrawable.TriggerDragEnd(state);
            draggingDrawable = null;

            return result;
        }

        private bool handleWheel(InputState state)
        {
            return mouseInputQueue.Any(target => target.TriggerWheel(state));
        }

        private bool handleKeyDown(InputState state, Key key, bool repeat)
        {
            KeyDownEventArgs args = new KeyDownEventArgs
            {
                Key = key,
                Repeat = repeat
            };

            if (!unfocusIfNoLongerValid(state))
            {
                if (args.Key == Key.Escape)
                {
                    FocusedDrawable.TriggerFocusLost(state);
                    return true;
                }
                if (FocusedDrawable.TriggerKeyDown(state, args))
                    return true;
            }

            return keyboardInputQueue.Any(target => target.TriggerKeyDown(state, args));
        }

        private bool handleKeyUp(InputState state, Key key)
        {
            KeyUpEventArgs args = new KeyUpEventArgs
            {
                Key = key
            };

            if (!unfocusIfNoLongerValid(state) && (FocusedDrawable?.TriggerKeyUp(state, args) ?? false))
                return true;

            return keyboardInputQueue.Any(target => target.TriggerKeyUp(state, args));
        }

        /// <summary>
        /// Unfocus the current focused drawable if it is no longer in a valid state.
        /// </summary>
        /// <returns>true if there is no longer a focus.</returns>
        private bool unfocusIfNoLongerValid(InputState state)
        {
            if (FocusedDrawable == null) return true;

            bool stillValid = FocusedDrawable.IsPresent && FocusedDrawable.Parent != null;

            if (stillValid)
            {
                //ensure we are visible
                IContainer d = FocusedDrawable.Parent;
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

            FocusedDrawable.TriggerFocusLost(state);
            FocusedDrawable = null;
            return true;
        }

        private void focusTopMostRequestingDrawable(InputState state) => keyboardInputQueue.FirstOrDefault(target => target.RequestingFocus)?.TriggerFocus(state, true);

        public InputHandler GetHandler(Type handlerType)
        {
            return inputHandlers.Find(h => h.GetType() == handlerType);
        }

        protected bool AddHandler(InputHandler handler)
        {
            try
            {
                if (handler.Initialize(Host))
                {
                    int index = inputHandlers.BinarySearch(handler, new InputHandlerComparer());
                    if (index < 0)
                    {
                        index = ~index;
                    }

                    inputHandlers.Insert(index, handler);

                    return true;
                }
            }
            catch
            {
            }

            return false;
        }

        protected bool RemoveHandler(InputHandler handler) => inputHandlers.Remove(handler);

        protected override void Dispose(bool isDisposing)
        {
            foreach (var h in inputHandlers)
                h.Dispose();

            base.Dispose(isDisposing);
        }
    }

    public enum ConfineMouseMode
    {
        Never,
        Fullscreen,
        Always
    }
}
