// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Input.StateChanges;
using osu.Framework.Platform;
using osu.Framework.Statistics;
using osuTK;
using osuTK.Input;

namespace osu.Framework.Input.Handlers.Mouse
{
    public class MouseHandler : InputHandler
    {
        public override bool IsActive => true;

        public override int Priority => 0;

        public override bool Initialize(GameHost host)
        {
            if (!(host.Window is Window window))
                return false;

            Enabled.BindValueChanged(e =>
            {
                if (e.NewValue)
                {
                    window.MouseMove += handleMouseMove;
                    window.MouseDown += handleMouseButton;
                    window.MouseUp += handleMouseButton;
                    window.MouseWheel += handleMouseWheel;
                }
                else
                {
                    window.MouseMove -= handleMouseMove;
                    window.MouseDown -= handleMouseButton;
                    window.MouseUp -= handleMouseButton;
                    window.MouseWheel -= handleMouseWheel;
                }
            }, true);

            return true;
        }

        private void enqueueInput(IInput input)
        {
            PendingInputs.Enqueue(input);
            FrameStatistics.Increment(StatisticsCounterType.MouseEvents);
        }

        private void handleMouseMove(MouseMoveInputArgs args) => enqueueInput(new MousePositionAbsoluteInput { Position = args.Position });

        private void handleMouseButton(MouseButtonInputArgs args) => enqueueInput(new MouseButtonInput(args.Button, args.Pressed));

        private void handleMouseWheel(MouseWheelInputArgs args) => enqueueInput(new MouseScrollRelativeInput { Delta = args.Delta, IsPrecise = args.Precise });
    }
}
