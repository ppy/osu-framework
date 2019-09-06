// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using osu.Framework.Graphics;
using osu.Framework.Input.Events;
using osu.Framework.Input.StateChanges;
using osu.Framework.Input.States;
using osu.Framework.Logging;
using osuTK;
using osuTK.Input;

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
        protected WeakReference<Drawable> ClickedDrawable = new WeakReference<Drawable>(null);

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

        public virtual void HandlePositionChange(InputState state, Vector2 lastPosition)
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
                    HandleMouseDrag(state, lastPosition);
                }
            }
        }

        protected bool BlockNextClick;

        public virtual void HandleButtonStateChange(InputState state, ButtonStateChangeKind kind, double currentTime)
        {
            Trace.Assert(state.Mouse.IsPressed(Button) == (kind == ButtonStateChangeKind.Pressed));

            if (kind == ButtonStateChangeKind.Pressed)
            {
                if (state.Mouse.IsPositionValid)
                    MouseDownPosition = state.Mouse.Position;

                HandleMouseDown(state);

                if (LastClickTime != null && currentTime - LastClickTime < DoubleClickTime)
                {
                    if (HandleMouseDoubleClick(state))
                    {
                        //when we handle a double-click we want to block a normal click from firing.
                        BlockNextClick = true;
                        LastClickTime = null;
                    }
                }
            }
            else
            {
                HandleMouseUp(state);

                if (EnableClick && DraggedDrawable == null)
                {
                    if (!BlockNextClick)
                    {
                        LastClickTime = currentTime;
                        HandleMouseClick(state);
                    }
                }

                BlockNextClick = false;

                if (EnableDrag)
                    HandleMouseDragEnd(state);

                MouseDownPosition = null;
                MouseDownInputQueue = null;
            }
        }

        protected virtual bool HandleMouseDown(InputState state)
        {
            var positionalInputQueue = PositionalInputQueue.ToList();
            var handledBy = PropagateMouseButtonEvent(positionalInputQueue, new MouseDownEvent(state, Button, MouseDownPosition));

            // only drawables up to the one that handled mouse down should handle mouse up
            MouseDownInputQueue = positionalInputQueue;

            if (handledBy != null)
            {
                var count = MouseDownInputQueue.IndexOf(handledBy) + 1;
                MouseDownInputQueue.RemoveRange(count, MouseDownInputQueue.Count - count);
            }

            return handledBy != null;
        }

        protected virtual bool HandleMouseUp(InputState state)
        {
            if (MouseDownInputQueue == null) return false;

            return PropagateMouseButtonEvent(MouseDownInputQueue, new MouseUpEvent(state, Button, MouseDownPosition)) != null;
        }

        protected virtual bool HandleMouseClick(InputState state)
        {
            if (MouseDownInputQueue == null) return false;

            // due to the laziness of IEnumerable, .Where check should be done right before it is triggered for the event.
            var drawables = MouseDownInputQueue.Intersect(PositionalInputQueue)
                                               .Where(t => t.IsAlive && t.IsPresent && t.ReceivePositionalInputAt(state.Mouse.Position));

            var clicked = PropagateMouseButtonEvent(drawables, new ClickEvent(state, Button, MouseDownPosition));
            ClickedDrawable.SetTarget(clicked);

            if (ChangeFocusOnClick)
                RequestFocus?.Invoke(clicked);

            if (clicked != null)
                Logger.Log($"MouseClick handled by {clicked}.", LoggingTarget.Runtime, LogLevel.Debug);

            return clicked != null;
        }

        protected virtual bool HandleMouseDoubleClick(InputState state)
        {
            if (!ClickedDrawable.TryGetTarget(out Drawable clicked))
                return false;

            if (!PositionalInputQueue.Contains(clicked))
                return false;

            return PropagateMouseButtonEvent(new[] { clicked }, new DoubleClickEvent(state, Button, MouseDownPosition)) != null;
        }

        protected virtual bool HandleMouseDrag(InputState state, Vector2 lastPosition)
        {
            if (DraggedDrawable == null) return false;

            //Once a drawable is dragged, it remains in a dragged state until the drag is finished.
            return PropagateMouseButtonEvent(new[] { DraggedDrawable }, new DragEvent(state, Button, MouseDownPosition, lastPosition)) != null;
        }

        protected virtual bool HandleMouseDragStart(InputState state)
        {
            Trace.Assert(DraggedDrawable == null, $"The {nameof(DraggedDrawable)} was not set to null by {nameof(HandleMouseDragEnd)}.");
            Trace.Assert(!DragStarted, $"A {nameof(DraggedDrawable)} was already searched for. Call {nameof(HandleMouseDragEnd)} first.");

            Trace.Assert(MouseDownPosition != null);

            DragStarted = true;

            // also the laziness of IEnumerable here
            var drawables = MouseDownInputQueue.Where(t => t.IsAlive && t.IsPresent);

            DraggedDrawable = PropagateMouseButtonEvent(drawables, new DragStartEvent(state, Button, MouseDownPosition));
            if (DraggedDrawable != null)
                DraggedDrawable.IsDragged = true;

            return DraggedDrawable != null;
        }

        protected virtual bool HandleMouseDragEnd(InputState state)
        {
            DragStarted = false;

            if (DraggedDrawable == null) return false;

            var result = PropagateMouseButtonEvent(new[] { DraggedDrawable }, new DragEndEvent(state, Button, MouseDownPosition)) != null;

            DraggedDrawable.IsDragged = false;
            DraggedDrawable = null;

            return result;
        }

        /// <summary>
        /// Triggers events on drawables in <paramref cref="drawables"/> until it is handled.
        /// </summary>
        /// <param name="drawables">The drawables in the queue.</param>
        /// <param name="e">The event.</param>
        /// <returns>The drawable which handled the event or null if none.</returns>
        protected virtual Drawable PropagateMouseButtonEvent(IEnumerable<Drawable> drawables, MouseButtonEvent e)
        {
            var handledBy = drawables.FirstOrDefault(target => target.TriggerEvent(e));

            if (handledBy != null)
                Logger.Log($"{e} handled by {handledBy}.", LoggingTarget.Runtime, LogLevel.Debug);

            return handledBy;
        }
    }
}
