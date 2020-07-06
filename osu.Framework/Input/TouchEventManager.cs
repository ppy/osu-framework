// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Diagnostics;
using osu.Framework.Graphics;
using osu.Framework.Input.Events;
using osu.Framework.Input.States;
using osuTK;

namespace osu.Framework.Input
{
    /// <summary>
    /// A manager that manages states and events for a single touch.
    /// </summary>
    public class TouchEventManager : ButtonEventManager<TouchSource>
    {
        protected Vector2? TouchDownPosition;

        public TouchEventManager(TouchSource source)
            : base(source)
        {
        }

        public void HandlePositionChange(InputState state, Vector2 lastPosition)
        {
            handleTouchMove(state, state.Touch.TouchPositions[(int)Button], lastPosition);
        }

        private void handleTouchMove(InputState state, Vector2 position, Vector2 lastPosition)
        {
            PropagateButtonEvent(ButtonDownInputQueue, new TouchMoveEvent(state, new Touch(Button, position), TouchDownPosition, lastPosition));
        }

        protected override Drawable HandleButtonDown(InputState state, List<Drawable> targets)
        {
            TouchDownPosition = state.Touch.GetTouchPosition(Button);
            Debug.Assert(TouchDownPosition != null);

            return PropagateButtonEvent(targets, new TouchDownEvent(state, new Touch(Button, (Vector2)TouchDownPosition)));
        }

        protected override void HandleButtonUp(InputState state, List<Drawable> targets)
        {
            var currentPosition = state.Touch.TouchPositions[(int)Button];
            PropagateButtonEvent(targets, new TouchUpEvent(state, new Touch(Button, currentPosition), TouchDownPosition));
            TouchDownPosition = null;
        }
    }
}
