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
    /// A manager that manages states and events for a single touch.
    /// </summary>
    public class TouchEventManager
    {
        /// <summary>
        /// The source of this touch event manager.
        /// </summary>
        public readonly MouseButton Source;

        /// <summary>
        /// A positional input queue built at a <see cref="TouchDownPosition"/> for propagating <see cref="Drawable.OnTouchUp"/>.
        /// </summary>
        protected List<Drawable> TouchDownInputQueue;

        protected Vector2? TouchDownPosition;

        public Func<Vector2, IEnumerable<Drawable>> GetTouchInputQueue;

        public TouchEventManager(MouseButton source)
        {
            if (source < MouseButton.Touch1)
                throw new ArgumentException($"Invalid touch source provided on constructor: {source}", nameof(source));

            Source = source;
        }

        public void HandlePositionChange(InputState state, Vector2 lastPosition)
        {
            var position = state.Touch.TouchPositions[Source];
            if (position == lastPosition)
                return;

            HandleTouchMove(state, position, lastPosition);
        }

        public void HandleActivityChange(InputState state, ButtonStateChangeKind kind)
        {
            Trace.Assert(state.Touch.IsActive(Source) == (kind == ButtonStateChangeKind.Pressed));

            var position = state.Touch.GetTouchPosition(Source);

            if (kind == ButtonStateChangeKind.Pressed)
            {
                TouchDownPosition = position;

                HandleTouchDown(state);
            }
            else
            {
                HandleTouchUp(state, position);

                TouchDownPosition = null;
                TouchDownInputQueue = null;
            }
        }

        protected virtual bool HandleTouchMove(InputState state, Vector2 position, Vector2 lastPosition)
        {
            return PropagateTouchEvent(GetTouchInputQueue(position), new TouchMoveEvent(state, new Touch(Source, position), TouchDownPosition, lastPosition)) != null;
        }

        protected virtual bool HandleTouchDown(InputState state)
        {
            if (!(TouchDownPosition is Vector2 downPos) || TouchDownInputQueue != null)
                return false;

            TouchDownInputQueue = GetTouchInputQueue(downPos).ToList();
            var handled = PropagateTouchEvent(TouchDownInputQueue, new TouchDownEvent(state, new Touch(Source, downPos)));

            if (handled == null)
                return false;

            var count = TouchDownInputQueue.IndexOf(handled) + 1;
            TouchDownInputQueue.RemoveRange(count, TouchDownInputQueue.Count - count);
            return true;
        }

        protected virtual bool HandleTouchUp(InputState state, Vector2? position)
        {
            if (!(position is Vector2 pos) || TouchDownInputQueue == null)
                return false;

            return PropagateTouchEvent(TouchDownInputQueue, new TouchUpEvent(state, new Touch(Source, pos), TouchDownPosition)) != null;
        }

        protected virtual Drawable PropagateTouchEvent(IEnumerable<Drawable> drawables, TouchEvent e)
        {
            var handled = drawables.FirstOrDefault(d => d.TriggerEvent(e));

            if (handled != null)
                Logger.Log($"{e} handled by {handled}", LoggingTarget.Runtime, LogLevel.Debug);

            return handled;
        }
    }
}
