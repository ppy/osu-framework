// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using osu.Framework.Extensions.EnumExtensions;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Graphics;
using osu.Framework.Input.Events;
using osu.Framework.Input.States;
using osu.Framework.Logging;
using osuTK;
using osuTK.Input;

namespace osu.Framework.Input
{
    /// <summary>
    /// Manages state and events (click, drag and double-click) for a single mouse button.
    /// </summary>
    public abstract class MouseButtonEventManager : ButtonEventManager<MouseButton>
    {
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

        /// <summary>
        /// Whether the next click should temporarily be ignored if enabled in this manager.
        /// This is required for double-click and touch long-press logic.
        /// </summary>
        internal bool BlockNextClick;

        protected MouseButtonEventManager(MouseButton button)
            : base(button)
        {
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
        protected WeakReference<Drawable> ClickedDrawable = new WeakReference<Drawable>(null!);

        /// <summary>
        /// Whether a drag operation has started and <see cref="DraggedDrawable"/> has been searched for.
        /// </summary>
        protected bool DragStarted;

        /// <summary>
        /// The <see cref="Drawable"/> which is currently being dragged. null if none is.
        /// </summary>
        public Drawable? DraggedDrawable { get; protected set; }

        public void HandlePositionChange(InputState state, Vector2 lastPosition)
        {
            if (EnableDrag)
            {
                if (!DragStarted)
                {
                    var mouse = state.Mouse;
                    if (mouse.IsPressed(Button) && Vector2Extensions.Distance(MouseDownPosition ?? mouse.Position, mouse.Position) > ClickDragDistance)
                        handleDragStart(state);
                }

                if (DragStarted)
                    handleDrag(state, lastPosition);
            }
        }

        protected override Drawable? HandleButtonDown(InputState state, List<Drawable> targets)
        {
            Trace.Assert(state.Mouse.IsPressed(Button));

            if (state.Mouse.IsPositionValid)
                MouseDownPosition = state.Mouse.Position;

            Drawable? handledBy = PropagateButtonEvent(targets, new MouseDownEvent(state, Button, MouseDownPosition));

            if (LastClickTime != null && InputManager.Time.Current - LastClickTime < DoubleClickTime)
            {
                if (handleDoubleClick(state, targets))
                {
                    //when we handle a double-click we want to block a normal click from firing.
                    BlockNextClick = true;
                    LastClickTime = null;
                }
            }

            return handledBy;
        }

        protected override void HandleButtonUp(InputState state, List<Drawable>? targets)
        {
            Trace.Assert(!state.Mouse.IsPressed(Button));

            if (targets != null)
                PropagateButtonEvent(targets, new MouseUpEvent(state, Button, MouseDownPosition));

            if (EnableClick && DraggedDrawable?.DragBlocksClick != true)
            {
                if (!BlockNextClick)
                {
                    LastClickTime = InputManager.Time.Current;
                    handleClick(state, targets);
                }
            }

            BlockNextClick = false;

            if (EnableDrag)
            {
                DragStarted = false;
                handleDragDrawableEnd(state);
            }

            MouseDownPosition = null;
        }

        private void handleClick(InputState state, List<Drawable>? targets)
        {
            if (targets == null) return;

            // due to the laziness of IEnumerable, .Where check should be done right before it is triggered for the event.
            var drawables = targets.Intersect(InputQueue)
                                   .Where(t => t.IsAlive && t.IsPresent && t.ReceivePositionalInputAt(state.Mouse.Position));

            InputManager.FocusedDrawableThisClick = null;

            Drawable? clicked = PropagateButtonEvent(drawables, new ClickEvent(state, Button, MouseDownPosition));
            ClickedDrawable.SetTarget(clicked!);

            // Focus shall only change if it wasn't explicitly changed during the click (for example, using a button to open a menu).
            if (InputManager.FocusedDrawableThisClick == null)
            {
                if (ChangeFocusOnClick && clicked?.ChangeFocusOnClick != false)
                    InputManager.ChangeFocusFromClick(clicked);
            }

            InputManager.FocusedDrawableThisClick = null;

            if (clicked != null)
                Logger.Log($"MouseClick handled by {clicked}.", LoggingTarget.Runtime, LogLevel.Debug);
        }

        private bool handleDoubleClick(InputState state, List<Drawable> targets)
        {
            if (!ClickedDrawable.TryGetTarget(out Drawable? clicked))
                return false;

            if (!targets.Contains(clicked))
                return false;

            return PropagateButtonEvent(new[] { clicked }, new DoubleClickEvent(state, Button, MouseDownPosition)) != null;
        }

        private void handleDrag(InputState state, Vector2 lastPosition)
        {
            if (DraggedDrawable == null) return;

            //Once a drawable is dragged, it remains in a dragged state until the drag is finished.
            PropagateButtonEvent(new[] { DraggedDrawable }, new DragEvent(state, Button, MouseDownPosition, lastPosition));
        }

        private void handleDragStart(InputState state)
        {
            Trace.Assert(DraggedDrawable == null, $"The {nameof(DraggedDrawable)} was not set to null by {nameof(handleDragDrawableEnd)}.");
            Trace.Assert(!DragStarted, $"A {nameof(DraggedDrawable)} was already searched for. Call {nameof(handleDragDrawableEnd)} first.");

            Trace.Assert(MouseDownPosition != null);

            DragStarted = true;

            // also the laziness of IEnumerable here
            var drawables = ButtonDownInputQueue.AsNonNull().Where(d => d.IsRootedAt(InputManager));

            var draggable = PropagateButtonEvent(drawables, new DragStartEvent(state, Button, MouseDownPosition));
            if (draggable != null)
                handleDragDrawableBegin(draggable);
        }

        private void handleDragDrawableBegin(Drawable drawable)
        {
            DraggedDrawable = drawable;
            DraggedDrawable.IsDragged = true;
            DraggedDrawable.Invalidated += draggedDrawableInvalidated;
        }

        private void draggedDrawableInvalidated(Drawable drawable, Invalidation invalidation)
        {
            if (invalidation.HasFlagFast(Invalidation.Parent))
            {
                // end drag if no longer rooted.
                if (!drawable.IsRootedAt(InputManager))
                    handleDragDrawableEnd();
            }
        }

        private void handleDragDrawableEnd(InputState? state = null)
        {
            var previousDragged = DraggedDrawable;

            if (previousDragged == null) return;

            previousDragged.Invalidated -= draggedDrawableInvalidated;
            previousDragged.IsDragged = false;

            DraggedDrawable = null;

            if (state != null)
                PropagateButtonEvent(new[] { previousDragged }, new DragEndEvent(state, Button, MouseDownPosition));
        }
    }
}
