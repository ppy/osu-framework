// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Input.StateChanges;
using osu.Framework.Platform;
using osu.Framework.Statistics;

namespace osu.Framework.Input.Handlers.Touch
{
    public class TouchHandler : InputHandler
    {
        public override bool IsActive => true;

        public override bool Initialize(GameHost host)
        {
            if (!base.Initialize(host))
                return false;

            if (!(host.Window is SDL2DesktopWindow window))
                return false;

            Enabled.BindValueChanged(enabled =>
            {
                if (enabled.NewValue)
                {
                    window.TouchDown += handleTouchDown;
                    window.TouchUp += handleTouchUp;
                }
                else
                {
                    window.TouchDown -= handleTouchDown;
                    window.TouchUp -= handleTouchUp;
                }
            }, true);

            return true;
        }

        private void handleTouchDown(Input.Touch touch)
        {
            enqueueTouch(touch, true);
        }

        private void handleTouchUp(Input.Touch touch)
        {
            enqueueTouch(touch, false);
        }

        private void enqueueTouch(Input.Touch touch, bool activate)
        {
            PendingInputs.Enqueue(new TouchInput(touch, activate));
            FrameStatistics.Increment(StatisticsCounterType.TouchEvents);
        }
    }
}
