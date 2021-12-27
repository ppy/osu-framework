// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Android.Views;
using osu.Framework.Input.Handlers;
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
    public class AndroidMouseHandler : InputHandler
    {
        public override string Description => "Mouse";

        public override bool IsActive => true;

        private readonly AndroidGameView view;

        public AndroidMouseHandler(AndroidGameView view)
        {
            this.view = view;
        }

        public override bool Initialize(GameHost host)
        {
            if (!base.Initialize(host))
                return false;

            Enabled.BindValueChanged(enabled =>
            {
                if (enabled.NewValue)
                {
                    view.MouseKeyDown += onKeyDown;
                    view.MouseKeyLongPress += onKeyDown;
                    view.MouseKeyUp += onKeyUp;
                    view.MouseHover += onHover;
                    view.MouseTouch += onTouch;
                    view.MouseGenericMotion += onGenericMotion;
                }
                else
                {
                    view.MouseKeyDown -= onKeyDown;
                    view.MouseKeyLongPress -= onKeyDown;
                    view.MouseKeyUp -= onKeyUp;
                    view.MouseHover -= onHover;
                    view.MouseTouch -= onTouch;
                    view.MouseGenericMotion -= onGenericMotion;
                }
            }, true);

            return true;
        }

        private void onKeyDown(Keycode keycode, KeyEvent e)
        {
            // some implementations might send Mouse1 and Mouse2 as keyboard keycodes, so we handle those here.
            if (keycode.TryGetMouseButton(out var button))
                handleMouseDown(button);
        }

        private void onKeyUp(Keycode keycode, KeyEvent e)
        {
            if (keycode.TryGetMouseButton(out var button))
                handleMouseUp(button);
        }

        private void onHover(MotionEvent hoverEvent)
        {
            switch (hoverEvent.Action)
            {
                case MotionEventActions.HoverMove:
                    handleMouseMoveEvent(hoverEvent);
                    break;
            }
        }

        private void onTouch(MotionEvent touchEvent)
        {
            switch (touchEvent.Action)
            {
                case MotionEventActions.Move:
                    handleMouseMoveEvent(touchEvent);
                    break;
            }
        }

        private void onGenericMotion(MotionEvent motionEvent)
        {
            switch (motionEvent.Action)
            {
                case MotionEventActions.ButtonPress:
                    handleMouseDown(motionEvent.ActionButton.ToMouseButton());
                    break;

                case MotionEventActions.ButtonRelease:
                    handleMouseUp(motionEvent.ActionButton.ToMouseButton());
                    break;

                case MotionEventActions.Scroll:
                    handleMouseWheel(getEventScroll(motionEvent));
                    break;
            }
        }

        private Vector2 getEventScroll(MotionEvent e) => new Vector2(e.GetAxisValue(Axis.Hscroll), e.GetAxisValue(Axis.Vscroll));

        private void handleMouseMoveEvent(MotionEvent evt)
        {
            // https://developer.android.com/reference/android/view/MotionEvent#batching
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
