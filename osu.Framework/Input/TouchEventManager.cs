// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Graphics;
using osu.Framework.Input.Events;
using osu.Framework.Input.States;
using osuTK;
using osuTK.Input;

namespace osu.Framework.Input
{
    /// <summary>
    /// A manager that manages states and events for a single touch.
    /// </summary>
    public class TouchEventManager : ButtonEventManager<MouseButton>
    {
        protected Vector2? TouchDownPosition;

        public TouchEventManager(MouseButton source)
            : base(source)
        {
        }

        public void HandlePositionChange(InputState state, Vector2 lastPosition)
        {
            var position = state.Touch.TouchPositions[Button];

            if (position == lastPosition)
                return;

            HandleTouchMove(state, position, lastPosition);
        }

        protected void HandleTouchMove(InputState state, Vector2 position, Vector2 lastPosition)
        {
            PropagateButtonEvent(InputQueue, new TouchMoveEvent(state, new Touch(Button, position), TouchDownPosition, lastPosition));
        }

        protected override Drawable HandleButtonDown(InputState state, List<Drawable> targets)
        {
            TouchDownPosition = state.Touch.GetTouchPosition(Button);

            if (TouchDownPosition is Vector2 downPosition && ButtonDownInputQueue == null)
                return PropagateButtonEvent(targets, new TouchDownEvent(state, new Touch(Button, downPosition)));

            return null;
        }

        protected override void HandleButtonUp(InputState state, List<Drawable> targets)
        {
            if (state.Touch.GetTouchPosition(Button) is Vector2 position && ButtonDownInputQueue != null)
                PropagateButtonEvent(targets, new TouchUpEvent(state, new Touch(Button, position), TouchDownPosition));

            TouchDownPosition = null;
        }
    }
}
