// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using Android.Views;
using osu.Framework.Input;
using osu.Framework.Input.StateChanges;
using osu.Framework.Input.States;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osuTK;
using osuTK.Input;

namespace osu.Framework.Android.Input
{
    public class AndroidTouchHandler : AndroidInputHandler
    {
        public override bool IsActive => true;

        protected override IEnumerable<InputSourceType> HandledEventSources => new[] { InputSourceType.BluetoothStylus, InputSourceType.Stylus, InputSourceType.Touchscreen };

        public AndroidTouchHandler(AndroidGameView view)
            : base(view)
        {
        }

        public override bool Initialize(GameHost host)
        {
            if (!base.Initialize(host))
                return false;

            Enabled.BindValueChanged(enabled =>
            {
                if (enabled.NewValue)
                {
                    View.Hover += HandleHover;
                    View.Touch += HandleTouch;
                }
                else
                {
                    View.Hover -= HandleHover;
                    View.Touch -= HandleTouch;
                }
            }, true);

            return true;
        }

        protected override void OnTouch(MotionEvent touchEvent)
        {
            Touch touch;

            switch (touchEvent.ActionMasked)
            {
                // MotionEventActions.Down arrives at the beginning of a touch event chain and implies the 0th pointer is pressed.
                // ActionIndex is generally not valid here.
                case MotionEventActions.Down:
                    touch = getEventTouch(touchEvent, 0);
                    PendingInputs.Enqueue(new TouchInput(touch, true));
                    break;

                // events that apply only to the ActionIndex pointer (other pointers' states remain unchanged)
                case MotionEventActions.PointerDown:
                case MotionEventActions.PointerUp:
                    if (touchEvent.ActionIndex < TouchState.MAX_TOUCH_COUNT)
                    {
                        touch = getEventTouch(touchEvent, touchEvent.ActionIndex);
                        PendingInputs.Enqueue(new TouchInput(touch, touchEvent.ActionMasked == MotionEventActions.PointerDown));
                    }

                    break;

                // events that apply to every pointer (up to PointerCount).
                case MotionEventActions.Move:
                case MotionEventActions.Up:
                case MotionEventActions.Cancel:
                    for (int i = 0; i < Math.Min(touchEvent.PointerCount, TouchState.MAX_TOUCH_COUNT); i++)
                    {
                        touch = getEventTouch(touchEvent, i);
                        PendingInputs.Enqueue(new TouchInput(touch, touchEvent.ActionMasked == MotionEventActions.Move));
                    }

                    break;

                default:
                    Logger.Log($"Unknown touch event action: {touchEvent.Action}, masked: {touchEvent.ActionMasked}");
                    break;
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
