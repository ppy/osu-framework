// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using Android.Views;
using osu.Framework.Input;
using osu.Framework.Input.StateChanges;
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

        protected override bool OnTouch(MotionEvent touchEvent)
        {
            switch (touchEvent.ActionMasked)
            {
                // MotionEventActions.Down arrives at the beginning of a touch event chain and implies the 0th pointer is pressed.
                // ActionIndex is generally not valid here.
                case MotionEventActions.Down:
                    applyTouchInput(touchEvent, HISTORY_CURRENT, 0);
                    return true;

                // events that apply only to the ActionIndex pointer (other pointers' states remain unchanged)
                case MotionEventActions.PointerDown:
                case MotionEventActions.PointerUp:
                    applyTouchInput(touchEvent, HISTORY_CURRENT, touchEvent.ActionIndex);
                    return true;

                // events that apply to every pointer (up to PointerCount).
                case MotionEventActions.Move:
                case MotionEventActions.Up:
                case MotionEventActions.Cancel:
                    touchEvent.HandleHistoricallyPerPointer(applyTouchInput);
                    return true;

                default:
                    return false;
            }
        }

        protected override bool OnHover(MotionEvent hoverEvent)
        {
            hoverEvent.HandleHistorically(apply);
            enqueueInput(new MouseButtonInput(MouseButton.Right, hoverEvent.IsButtonPressed(MotionEventButtonState.StylusPrimary)));

            // TODO: handle stylus events based on hoverEvent.Action
            // stylus should probably have it's own handler.
            return true;

            void apply(MotionEvent e, int historyPosition)
            {
                if (tryGetEventPosition(e, historyPosition, 0, out var position))
                    enqueueInput(new MousePositionAbsoluteInput { Position = position });
            }
        }

        private void applyTouchInput(MotionEvent touchEvent, int historyPosition, int pointerIndex)
        {
            if (tryGetEventTouch(touchEvent, historyPosition, pointerIndex, out var touch))
                enqueueInput(new TouchInput(touch, touchEvent.ActionMasked.IsTouchDownAction()));
        }

        private bool tryGetEventTouch(MotionEvent motionEvent, int historyPosition, int pointerIndex, out Touch touch)
        {
            if (tryGetTouchSource(motionEvent.GetPointerId(pointerIndex), out var touchSource)
                && tryGetEventPosition(motionEvent, historyPosition, pointerIndex, out var position))
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

        private bool tryGetEventPosition(MotionEvent motionEvent, int historyPosition, int pointerIndex, out Vector2 position)
        {
            if (motionEvent.TryGet(Axis.X, out float x, historyPosition, pointerIndex)
                && motionEvent.TryGet(Axis.Y, out float y, historyPosition, pointerIndex))
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
