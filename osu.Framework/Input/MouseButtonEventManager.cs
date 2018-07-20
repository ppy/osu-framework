// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using osu.Framework.Graphics;
using OpenTK;
using OpenTK.Input;
using System.Linq;
using System.Diagnostics;
using osu.Framework.Input.EventArgs;
using osu.Framework.Input.StateChanges;
using osu.Framework.Input.States;
using osu.Framework.Logging;

namespace osu.Framework.Input
{
    /// <summary>
    /// Manages state and events (click, drag and double-click) for a single mouse button.
    /// </summary>
    public abstract class MouseButtonEventManager
    {
        /// <summary>
        /// The mouse button this manager manages.
        /// </summary>
        public readonly MouseButton Button;

        /// <summary>
        /// Used for requesting focus from click.
        /// </summary>
        public Action<Drawable> RequestFocus;

        /// <summary>
        /// Used for get a positional input queue.
        /// </summary>
        public Func<IEnumerable<Drawable>> GetPositionalInputQueue;

        /// <summary>
        /// Whether dragging is handled by the managed button.
        /// </summary>
        public abstract bool EnableDrag { get; }

        /// <summary>
        /// Whether click and double click are handled by the managed button.
        /// </summary>
        public abstract bool EnableClick { get; }

        /// <summary>
        /// Whether focus is changed when the button is clicked.
        /// </summary>
        public abstract bool ChangeFocusOnClick { get; }

        protected MouseButtonEventManager(MouseButton button)
        {
            Button = button;
        }

        /// <summary>
        /// The maximum time between two clicks for a double-click to be considered.
        /// </summary>
        public virtual float DoubleClickTime => 250;

        /// <summary>
        /// The distance that must be moved until a dragged click becomes invalid.
        /// </summary>
        public virtual float ClickDragDistance => 10;

        /// <summary>
        /// The position of the mouse when the last time the button is pressed.
        /// </summary>
        public Vector2? MouseDownPosition { get; protected set; }

        /// <summary>
        /// The time of last click.
        /// </summary>
        protected double? LastClickTime;

        /// <summary>
        /// The drawable which is clicked by the last click.
        /// </summary>
        protected Drawable ClickedDrawable;

        /// <summary>
        /// Whether a drag operation has started and <see cref="DraggedDrawable"/> has been searched for.
        /// </summary>
        protected bool DragStarted;

        /// <summary>
        /// The positional input queue.
        /// </summary>
        protected IEnumerable<Drawable> PositionalInputQueue => GetPositionalInputQueue?.Invoke() ?? Enumerable.Empty<Drawable>();

        /// <summary>
        /// The input queue for propagating <see cref="Drawable.OnMouseUp"/>.
        /// This is created from the <see cref="PositionalInputQueue"/> when the last time the button is pressed.
        /// </summary>
        protected List<Drawable> MouseDownInputQueue;

        /// <summary>
        /// The <see cref="Drawable"/> which is currently being dragged. null if none is.
        /// </summary>
        public Drawable DraggedDrawable { get; protected set; }

        public virtual void HandlePositionChange(InputState state)
        {
            if (EnableDrag)
            {
                if (!DragStarted)
                {
                    var mouse = state.Mouse;
                    if (mouse.IsPressed(Button) && Vector2Extensions.Distance(MouseDownPosition ?? mouse.Position, mouse.Position) > ClickDragDistance)
                        HandleMouseDragStart(state);
                }
                else
                {
                    HandleMouseDrag(state);
                }
            }
        }

        public virtual void HandleButtonStateChange(InputState state, ButtonStateChangeKind kind, double currentTime)
        {
            Trace.Assert(state.Mouse.IsPressed(Button) == (kind == ButtonStateChangeKind.Pressed));

            if (kind == ButtonStateChangeKind.Pressed)
            {
                if (state.Mouse.IsPositionValid)
                    MouseDownPosition = state.Mouse.Position;
                HandleMouseDown(state);
            }
            else
            {
                HandleMouseUp(state);

                if (EnableClick && DraggedDrawable == null)
                {
                    bool isValidClick = true;
                    if (LastClickTime != null && currentTime - LastClickTime < DoubleClickTime)
                    {
                        if (HandleMouseDoubleClick(state))
                        {
                            //when we handle a double-click we want to block a normal click from firing.
                            isValidClick = false;
                            LastClickTime = null;
                        }
                    }

                    if (isValidClick)
                    {
                        LastClickTime = currentTime;
                        HandleMouseClick(state);
                    }
                }

                if (EnableDrag)
                    HandleMouseDragEnd(state);

                MouseDownPosition = null;
            }
        }

        protected virtual bool HandleMouseDown(InputState state)
        {
            MouseDownEventArgs args = new MouseDownEventArgs { Button = Button };

            var positionalInputQueue = PositionalInputQueue.ToList();
            var result = PropagateMouseDown(positionalInputQueue, state, args, out Drawable handledBy);

            // only drawables up to the one that handled mouse down should handle mouse up
            MouseDownInputQueue = positionalInputQueue;
            if (result)
            {
                var count = MouseDownInputQueue.IndexOf(handledBy) + 1;
                MouseDownInputQueue.RemoveRange(count, MouseDownInputQueue.Count - count);
            }

            return result;
        }

        // set PositionMouseDown right before the event is invoked so it can be used as the mouse down position for each button
        private void setPositionMouseDown(InputState state)
        {
            state.Mouse.PositionMouseDown = MouseDownPosition;
        }

        protected virtual bool HandleMouseUp(InputState state)
        {
            if (MouseDownInputQueue == null) return false;

            MouseUpEventArgs args = new MouseUpEventArgs { Button = Button };

            setPositionMouseDown(state);

            return PropagateMouseUp(MouseDownInputQueue, state, args);
        }

        protected virtual bool HandleMouseClick(InputState state)
        {
            var intersectingQueue = MouseDownInputQueue.Intersect(PositionalInputQueue);

            setPositionMouseDown(state);

            // click pass, triggering an OnClick on all drawables up to the first which returns true.
            // an extra IsHovered check is performed because we are using an outdated queue (for valid reasons which we need to document).
            ClickedDrawable = intersectingQueue.FirstOrDefault(t => t.CanReceiveMouseInput && t.ReceiveMouseInputAt(state.Mouse.Position) && t.TriggerOnClick(state));

            if (ChangeFocusOnClick)
                RequestFocus?.Invoke(ClickedDrawable);

            if (ClickedDrawable != null)
                Logger.Log($"MouseClick handled by {ClickedDrawable}.", LoggingTarget.Runtime, LogLevel.Debug);

            return ClickedDrawable != null;
        }

        protected virtual bool HandleMouseDoubleClick(InputState state)
        {
            if (ClickedDrawable == null) return false;

            setPositionMouseDown(state);

            return ClickedDrawable.ReceiveMouseInputAt(state.Mouse.Position) && ClickedDrawable.TriggerOnDoubleClick(state);
        }

        protected virtual bool HandleMouseDrag(InputState state)
        {
            setPositionMouseDown(state);

            //Once a drawable is dragged, it remains in a dragged state until the drag is finished.
            return DraggedDrawable?.TriggerOnDrag(state) ?? false;
        }

        protected virtual bool HandleMouseDragStart(InputState state)
        {
            Trace.Assert(DraggedDrawable == null, $"The {nameof(DraggedDrawable)} was not set to null by {nameof(HandleMouseDragEnd)}.");
            Trace.Assert(!DragStarted, $"A {nameof(DraggedDrawable)} was already searched for. Call {nameof(HandleMouseDragEnd)} first.");

            Trace.Assert(MouseDownPosition != null);

            DragStarted = true;

            setPositionMouseDown(state);

            DraggedDrawable = MouseDownInputQueue?.FirstOrDefault(target => target.IsAlive && target.IsPresent && target.TriggerOnDragStart(state));
            if (DraggedDrawable != null)
            {
                DraggedDrawable.IsDragged = true;
                Logger.Log($"MouseDragStart handled by {DraggedDrawable}.", LoggingTarget.Runtime, LogLevel.Debug);
            }

            return DraggedDrawable != null;
        }

        protected virtual bool HandleMouseDragEnd(InputState state)
        {
            DragStarted = false;

            if (DraggedDrawable == null)
                return false;

            setPositionMouseDown(state);

            bool result = DraggedDrawable.TriggerOnDragEnd(state);
            DraggedDrawable.IsDragged = false;
            DraggedDrawable = null;

            return result;
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
    }
}
