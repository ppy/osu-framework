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

        protected override IEnumerable<InputSourceType> HandledInputSources => new[] { InputSourceType.BluetoothStylus, InputSourceType.Stylus, InputSourceType.Touchscreen };

        public AndroidTouchHandler(AndroidGameView view)
            : base(view)
        {
            View.Touch += handleTouch;
            View.Hover += handleHover;
        }

        private void handleTouch(object sender, View.TouchEventArgs e)
        {
            if (!ShouldHandleEvent(e.Event)) return;

            if (e.Event.Action == MotionEventActions.Move)
            {
                for (int i = 0; i < Math.Min(e.Event.PointerCount, TouchState.MAX_TOUCH_COUNT); i++)
                {
                    var touch = getEventTouch(e.Event, i);
                    PendingInputs.Enqueue(new TouchInput(touch, true));
                }
            }
            else if (e.Event.ActionIndex < TouchState.MAX_TOUCH_COUNT)
            {
                var touch = getEventTouch(e.Event, e.Event.ActionIndex);

                switch (e.Event.ActionMasked)
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

        private void handleHover(object sender, View.HoverEventArgs e)
        {
            if (!ShouldHandleEvent(e.Event)) return;

            PendingInputs.Enqueue(new MousePositionAbsoluteInput { Position = getEventPosition(e.Event) });
            PendingInputs.Enqueue(new MouseButtonInput(MouseButton.Right, e.Event.IsButtonPressed(MotionEventButtonState.StylusPrimary)));
        }

        private Touch getEventTouch(MotionEvent e, int index) => new Touch((TouchSource)e.GetPointerId(index), getEventPosition(e, index));
        private Vector2 getEventPosition(MotionEvent e, int index = 0) => new Vector2(e.GetX(index) * View.ScaleX, e.GetY(index) * View.ScaleY);

        protected override void Dispose(bool disposing)
        {
            View.Touch -= handleTouch;
            View.Hover -= handleHover;
            base.Dispose(disposing);
        }
    }
}
