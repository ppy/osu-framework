// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using Android.OS;
using Android.Views;
using osu.Framework.Bindables;
using osu.Framework.Extensions.EnumExtensions;
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
        /// <summary>
        /// Whether relative mode should be preferred when the window has focus, the cursor is contained and the OS cursor is not visible.
        /// </summary>
        /// <remarks>
        /// Only available in Android 8.0 Oreo (<see cref="BuildVersionCodes.O"/>) and up.
        /// </remarks>
        public BindableBool UseRelativeMode { get; } = new BindableBool(true)
        {
            Description = "Allows for sensitivity adjustment and tighter control of input",
        };

        public BindableDouble Sensitivity { get; } = new BindableDouble(1)
        {
            MinValue = 0.1,
            MaxValue = 10,
            Precision = 0.01,
        };

        public override string Description => "Mouse";

        public override bool IsActive => true;

        protected override IEnumerable<InputSourceType> HandledEventSources => new[] { InputSourceType.Mouse, InputSourceType.MouseRelative, InputSourceType.Touchpad };

        private AndroidGameWindow window;

        /// <summary>
        /// Whether a non-relative mouse event has ever been received.
        /// This is used as a starting location for relative movement.
        /// </summary>
        private bool absolutePositionReceived;

        public AndroidMouseHandler(AndroidGameView view)
            : base(view)
        {
        }

        public override bool Initialize(GameHost host)
        {
            if (!base.Initialize(host))
                return false;

            if (!(host.Window is AndroidGameWindow androidWindow))
                return false;

            window = androidWindow;

            window.CursorStateChanged += updatePointerCapture;

            // it's possible that Android forcefully released capture if we were unfocused.
            // so we update here when we get focus again.
            View.FocusChange += (sender, args) =>
            {
                if (args.HasFocus)
                    updatePointerCapture();
            };

            UseRelativeMode.BindValueChanged(_ => updatePointerCapture());

            Enabled.BindValueChanged(enabled =>
            {
                if (enabled.NewValue)
                {
                    View.GenericMotion += HandleGenericMotion;
                    View.Hover += HandleHover;
                    View.KeyDown += HandleKeyDown;
                    View.KeyUp += HandleKeyUp;
                    View.Touch += HandleTouch;

                    // Pointer capture is only available on Android 8.0 and up
                    if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
                        View.CapturedPointer += HandleCapturedPointer;
                }
                else
                {
                    View.GenericMotion -= HandleGenericMotion;
                    View.Hover -= HandleHover;
                    View.KeyDown -= HandleKeyDown;
                    View.KeyUp -= HandleKeyUp;
                    View.Touch -= HandleTouch;

                    // Pointer capture is only available on Android 8.0 and up
                    if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
                        View.CapturedPointer -= HandleCapturedPointer;
                }

                updatePointerCapture();
            }, true);

            return true;
        }

        public override void Reset()
        {
            Sensitivity.SetDefault();
            base.Reset();
        }

        private void updatePointerCapture()
        {
            // Pointer capture is only available on Android 8.0 and up
            if (Build.VERSION.SdkInt < BuildVersionCodes.O)
                return;

            bool shouldCapture =
                // check whether this handler is actually enabled.
                Enabled.Value
                // check whether the consumer has requested to use relative mode when feasible.
                && UseRelativeMode.Value
                // relative mode requires at least one absolute input to arrive, to gain an additional position to work with.
                && absolutePositionReceived
                // relative mode shouldn't ever be enabled if the framework or a consumer has chosen not to hide the cursor.
                && window.CursorState.HasFlagFast(CursorState.Hidden);

            View.PointerCapture = shouldCapture;
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

        protected override void OnCapturedPointer(MotionEvent capturedPointerEvent)
        {
            switch (capturedPointerEvent.Action)
            {
                case MotionEventActions.Move:
                    handleMouseMoveRelativeEvent(capturedPointerEvent);
                    break;

                case MotionEventActions.Scroll:
                    handleMouseWheel(getEventScroll(capturedPointerEvent));
                    break;

                case MotionEventActions.ButtonPress:
                    handleMouseDown(capturedPointerEvent.ActionButton.ToMouseButton());
                    break;

                case MotionEventActions.ButtonRelease:
                    handleMouseUp(capturedPointerEvent.ActionButton.ToMouseButton());
                    break;
            }
        }

        private Vector2 getEventScroll(MotionEvent e) => new Vector2(e.GetAxisValue(Axis.Hscroll), e.GetAxisValue(Axis.Vscroll));

        private void handleMouseMoveEvent(MotionEvent evt)
        {
            // https://developer.android.com/reference/android/View/MotionEvent#batching
            for (int i = 0; i < evt.HistorySize; i++)
                handleMouseMove(new Vector2(evt.GetHistoricalX(i), evt.GetHistoricalY(i)));

            handleMouseMove(new Vector2(evt.GetX(), evt.GetY()));

            absolutePositionReceived = true;

            // we may lose pointer capture if we lose focus / the app goes to the background,
            // so we use this opportunity to update capture if the user has requested it.
            updatePointerCapture();
        }

        private void handleMouseMoveRelativeEvent(MotionEvent evt)
        {
            for (int i = 0; i < evt.HistorySize; i++)
                handleMouseMoveRelative(new Vector2(evt.GetHistoricalX(i), evt.GetHistoricalY(i)));

            handleMouseMoveRelative(new Vector2(evt.GetX(), evt.GetY()));
        }

        private void handleMouseMove(Vector2 position) => enqueueInput(new MousePositionAbsoluteInput { Position = position });

        private void handleMouseMoveRelative(Vector2 delta) => enqueueInput(new MousePositionRelativeInput { Delta = delta * (float)Sensitivity.Value });

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
