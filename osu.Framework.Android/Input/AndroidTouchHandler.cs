// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using Android.Views;
using osu.Framework.Input;
using osu.Framework.Input.StateChanges;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osu.Framework.Statistics;
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
                    if (tryGetEventTouch(touchEvent, 0, out touch))
                        enqueueInput(new TouchInput(touch, true));
                    break;

                // events that apply only to the ActionIndex pointer (other pointers' states remain unchanged)
                case MotionEventActions.PointerDown:
                case MotionEventActions.PointerUp:
                    if (tryGetEventTouch(touchEvent, touchEvent.ActionIndex, out touch))
                        enqueueInput(new TouchInput(touch, touchEvent.ActionMasked == MotionEventActions.PointerDown));

                    break;

                // events that apply to every pointer (up to PointerCount).
                case MotionEventActions.Move:
                case MotionEventActions.Up:
                case MotionEventActions.Cancel:
                    for (int i = 0; i < touchEvent.PointerCount; i++)
                    {
                        if (tryGetEventTouch(touchEvent, i, out touch))
                            enqueueInput(new TouchInput(touch, touchEvent.ActionMasked == MotionEventActions.Move));
                    }

                    break;

                default:
                    Logger.Log($"Unknown touch event action: {touchEvent.Action}, masked: {touchEvent.ActionMasked}");
                    break;
            }
        }

        protected override void OnHover(MotionEvent hoverEvent)
        {
            if (tryGetEventPosition(hoverEvent, 0, out var position))
                enqueueInput(new MousePositionAbsoluteInput { Position = position });
            enqueueInput(new MouseButtonInput(MouseButton.Right, hoverEvent.IsButtonPressed(MotionEventButtonState.StylusPrimary)));
        }

        private bool tryGetEventTouch(MotionEvent e, int index, out Touch touch)
        {
            if (tryGetTouchSource(e.GetPointerId(index), out var touchSource)
                && tryGetEventPosition(e, index, out var position))
            {
                touch = new Touch(touchSource, position);
                return true;
            }

            touch = new Touch();
            return false;

            bool tryGetTouchSource(int pointerId, out TouchSource source)
            {
                source = (TouchSource)pointerId;
                return source >= TouchSource.Touch1 && source <= TouchSource.Touch10;
            }
        }

        private bool tryGetEventPosition(MotionEvent e, int index, out Vector2 position)
        {
            if (e.TryGet(Axis.X, out float x, pointerIndex: index)
                && e.TryGet(Axis.Y, out float y, pointerIndex: index))
            {
                position = new Vector2(x * View.ScaleX, y * View.ScaleY);
                return true;
            }

            // in empirical testing, `MotionEvent.Get{X,Y}()` methods can return NaN positions early on in the android activity's lifetime.
            // these nonsensical inputs then cause issues later down the line when they are converted into framework inputs.
            // as there is really nothing to recover from such inputs, drop them entirely.
            position = Vector2.Zero;
            return false;
        }

        private void enqueueInput(IInput input)
        {
            PendingInputs.Enqueue(input);
            FrameStatistics.Increment(StatisticsCounterType.TouchEvents);
        }
    }
}
