// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using Android.Views;
using osu.Framework.Input.StateChanges;
using osu.Framework.Platform;
using osu.Framework.Statistics;
using osuTK;
using osuTK.Input;

namespace osu.Framework.Android.Input
{
    /// <summary>
    /// Input handler for Android mouse-type devices: the <see cref="InputSourceType.Mouse"/> and <see cref="InputSourceType.Touchpad"/>.
    /// </summary>
    public class AndroidMouseHandler : AndroidInputHandler
    {
        public override string Description => "Mouse";

        public override bool IsActive => true;

        protected override IEnumerable<InputSourceType> HandledEventSources => new[] { InputSourceType.Mouse, InputSourceType.Touchpad };

        public AndroidMouseHandler(AndroidGameView view)
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
                    View.GenericMotion += HandleGenericMotion;
                    View.Hover += HandleHover;
                    View.KeyDown += HandleKeyDown;
                    View.KeyUp += HandleKeyUp;
                    View.Touch += HandleTouch;
                }
                else
                {
                    View.GenericMotion -= HandleGenericMotion;
                    View.Hover -= HandleHover;
                    View.KeyDown -= HandleKeyDown;
                    View.KeyUp -= HandleKeyUp;
                    View.Touch -= HandleTouch;
                }
            }, true);

            return true;
        }

        protected override void OnKeyDown(Keycode keycode, KeyEvent e)
        {
            // some implementations might send Mouse1 and Mouse2 as keyboard keycodes, so we handle those here.
            if (keycode.TryGetMouseButton(out var button))
                handleMouseDown(button);
        }

        protected override void OnKeyUp(Keycode keycode, KeyEvent e)
        {
            if (keycode.TryGetMouseButton(out var button))
                handleMouseUp(button);
        }

        protected override void OnHover(MotionEvent hoverEvent)
        {
            switch (hoverEvent.Action)
            {
                case MotionEventActions.HoverMove:
                    handleMouseMoveEvent(hoverEvent);
                    break;
            }
        }

        protected override void OnTouch(MotionEvent touchEvent)
        {
            switch (touchEvent.Action)
            {
                case MotionEventActions.Move:
                    handleMouseMoveEvent(touchEvent);
                    break;
            }
        }

        protected override void OnGenericMotion(MotionEvent genericMotionEvent)
        {
            switch (genericMotionEvent.Action)
            {
                case MotionEventActions.ButtonPress:
                    handleMouseDown(genericMotionEvent.ActionButton.ToMouseButton());
                    break;

                case MotionEventActions.ButtonRelease:
                    handleMouseUp(genericMotionEvent.ActionButton.ToMouseButton());
                    break;

                case MotionEventActions.Scroll:
                    handleMouseWheel(getEventScroll(genericMotionEvent));
                    break;
            }
        }

        private Vector2 getEventScroll(MotionEvent e) => new Vector2(e.GetAxisValue(Axis.Hscroll), e.GetAxisValue(Axis.Vscroll));

        private void handleMouseMoveEvent(MotionEvent evt)
        {
            // https://developer.android.com/reference/android/View/MotionEvent#batching
            for (int i = 0; i < evt.HistorySize; i++)
                handleMouseMove(new Vector2(evt.GetHistoricalX(i), evt.GetHistoricalY(i)));

            handleMouseMove(new Vector2(evt.RawX, evt.RawY));
        }

        private void handleMouseMove(Vector2 position) => enqueueInput(new MousePositionAbsoluteInput { Position = position });

        private void handleMouseDown(MouseButton button) => enqueueInput(new MouseButtonInput(button, true));

        private void handleMouseUp(MouseButton button) => enqueueInput(new MouseButtonInput(button, false));

        private void handleMouseWheel(Vector2 delta) => enqueueInput(new MouseScrollRelativeInput { Delta = delta });

        private void enqueueInput(IInput input)
        {
            PendingInputs.Enqueue(input);
            FrameStatistics.Increment(StatisticsCounterType.MouseEvents);
        }
    }
}
