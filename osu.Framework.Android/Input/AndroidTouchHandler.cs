// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using Android.Views;
using osu.Framework.Input;
using osu.Framework.Input.StateChanges;
using osu.Framework.Input.States;
using osuTK;
using osuTK.Input;

namespace osu.Framework.Android.Input
{
    public class AndroidTouchHandler : AndroidInputHandler
    {
        public override bool IsActive => true;

        protected override IEnumerable<InputSourceType> HandledEventSources => new[] { InputSourceType.BluetoothStylus, InputSourceType.Stylus, InputSourceType.Touchscreen };

        protected override IEnumerable<InputEventType> HandledEventTypes => new[] { InputEventType.Hover, InputEventType.Touch };

        public AndroidTouchHandler(AndroidGameView view)
            : base(view)
        {
        }

        protected override void OnTouch(MotionEvent touchEvent)
        {
            if (touchEvent.Action == MotionEventActions.Move)
            {
                for (int i = 0; i < Math.Min(touchEvent.PointerCount, TouchState.MAX_TOUCH_COUNT); i++)
                {
                    var touch = getEventTouch(touchEvent, i);
                    PendingInputs.Enqueue(new TouchInput(touch, true));
                }
            }
            else if (touchEvent.ActionIndex < TouchState.MAX_TOUCH_COUNT)
            {
                var touch = getEventTouch(touchEvent, touchEvent.ActionIndex);

                switch (touchEvent.ActionMasked)
                {
                    case MotionEventActions.Down:
                    case MotionEventActions.PointerDown:
                        PendingInputs.Enqueue(new TouchInput(touch, true));
                        break;

                    case MotionEventActions.Up:
                    case MotionEventActions.PointerUp:
                    case MotionEventActions.Cancel:
                        PendingInputs.Enqueue(new TouchInput(touch, false));
                        break;
                }
            }
        }

        protected override void OnHover(MotionEvent hoverEvent)
        {
            PendingInputs.Enqueue(new MousePositionAbsoluteInput { Position = getEventPosition(hoverEvent) });
            PendingInputs.Enqueue(new MouseButtonInput(MouseButton.Right, hoverEvent.IsButtonPressed(MotionEventButtonState.StylusPrimary)));
        }

        private Touch getEventTouch(MotionEvent e, int index) => new Touch((TouchSource)e.GetPointerId(index), getEventPosition(e, index));
        private Vector2 getEventPosition(MotionEvent e, int index = 0) => new Vector2(e.GetX(index) * View.ScaleX, e.GetY(index) * View.ScaleY);
    }
}
