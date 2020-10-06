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
                    window.MouseDown += handleMouseDown;
                    window.MouseUp += handleMouseUp;
                    window.MouseWheel += handleMouseWheel;
                }
                else
                {
                    window.MouseMove -= handleMouseMove;
                    window.MouseDown -= handleMouseDown;
                    window.MouseUp -= handleMouseUp;
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

        private void handleMouseMove(Vector2 position) => enqueueInput(new MousePositionAbsoluteInput { Position = position });

        private void handleMouseDown(MouseButton button) => enqueueInput(new MouseButtonInput(button, true));

        private void handleMouseUp(MouseButton button) => enqueueInput(new MouseButtonInput(button, false));

        private void handleMouseWheel(Vector2 delta, bool precise) => enqueueInput(new MouseScrollRelativeInput { Delta = delta, IsPrecise = precise });
    }
}
