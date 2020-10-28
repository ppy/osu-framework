// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Bindables;
using osu.Framework.Input.StateChanges;
using osu.Framework.Platform;
using osu.Framework.Statistics;
using osuTK;
using osuTK.Input;

namespace osu.Framework.Input.Handlers.Mouse
{
    public class RawMouseHandler : InputHandler, IHasCursorSensitivity, INeedsMousePositionFeedback
    {
        public BindableDouble Sensitivity { get; } = new BindableDouble(1) { MinValue = 0.1, MaxValue = 10 };

        public override bool IsActive => true;
        public override int Priority => 0;

        private DesktopWindow window;

        // private readonly BindableBool mapAbsoluteInputToWindow = new BindableBool();
        // private readonly IBindable<bool> windowActive = new BindableBool();

        public override bool Initialize(GameHost host)
        {
            if (!(host.Window is DesktopWindow desktopWindow))
                return false;

            window = desktopWindow;
            // mapAbsoluteInputToWindow.BindTo(window.MapAbsoluteInputToWindow);
            // windowActive.BindTo(window.IsActive);
            // windowActive.ValueChanged += _ => window.UpdateRelativeMode();

            Enabled.BindValueChanged(evt =>
            {
                if (evt.NewValue)
                {
                    window.MouseMove += handleMouseMove;
                    window.MouseMoveRelative += handleMouseMoveRelative;
                    window.MouseDown += handleMouseDown;
                    window.MouseUp += handleMouseUp;
                    window.MouseWheel += handleMouseWheel;
                }
                else
                {
                    window.MouseMove -= handleMouseMove;
                    window.MouseMoveRelative -= handleMouseMoveRelative;
                    window.MouseDown -= handleMouseDown;
                    window.MouseUp -= handleMouseUp;
                    window.MouseWheel -= handleMouseWheel;
                }

                window.RelativeMouseMode = evt.NewValue;
            });

            return true;
        }

        private void enqueueInput(IInput input)
        {
            PendingInputs.Enqueue(input);
            FrameStatistics.Increment(StatisticsCounterType.MouseEvents);
        }

        private void handleMouseMove(Vector2 position) => enqueueInput(new MousePositionAbsoluteInput { Position = position });

        private void handleMouseMoveRelative(Vector2 delta) => enqueueInput(new MousePositionRelativeInput { Delta = delta * (float)Sensitivity.Value });

        private void handleMouseDown(MouseButton button) => enqueueInput(new MouseButtonInput(button, true));

        private void handleMouseUp(MouseButton button) => enqueueInput(new MouseButtonInput(button, false));

        private void handleMouseWheel(Vector2 delta, bool precise) => enqueueInput(new MouseScrollRelativeInput { Delta = delta, IsPrecise = precise });

        public void FeedbackMousePositionChange(Vector2 position)
        {
            if (!Enabled.Value)
                return;

            window.UpdateRelativeMode(position);
        }
    }
}
