// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Bindables;
using osu.Framework.Input.StateChanges;
using osu.Framework.Platform;
using osu.Framework.Statistics;
using osuTK;

namespace osu.Framework.Input.Handlers.Trackpad
{
    /// <summary>
    /// Handles mouse events from an <see cref="SDL2DesktopWindow"/>.
    /// Will use relative mouse mode where possible.
    /// </summary>
    public class TrackpadHandler : InputHandler
    {
        public override string Description => "Trackpad";

        public override bool IsActive => true;

        private SDL2DesktopWindow window;

        public override bool Initialize(GameHost host)
        {
            if (!base.Initialize(host))
                return false;

            if (!(host.Window is SDL2DesktopWindow desktopWindow))
                return false;

            window = desktopWindow;

            Enabled.BindValueChanged(enabled =>
            {
                if (enabled.NewValue)
                {
                    window.TrackpadPositionChanged += HandleTrackpadMove;
                }
                else
                {
                    window.TrackpadPositionChanged -= HandleTrackpadMove;
                }
            }, true);

            return true;
        }

        public void HandleTrackpadMove(Vector2 vector)
        {
            // todo: Proper transformations... like tablet
            float sizeFactor = 0.55f;


            float trackpadX = (window.Size.Width / (2 * sizeFactor)) * ((2 * vector.X) - 1);
            float trackpadY = (window.Size.Height / (2 * sizeFactor)) * ((2 * (1 - vector.Y)) - 1);

            enqueueInput(new MousePositionAbsoluteInput() { Position = new Vector2(trackpadX + (window.Size.Width / 2), trackpadY + (window.Size.Height / 2)) });
        }

        public override void Reset()
        {
            // Sensitivity.SetDefault();
            // todo resets areaoffset and areasize
            base.Reset();
        }

        private void enqueueInput(IInput input)
        {
            PendingInputs.Enqueue(input);
            FrameStatistics.Increment(StatisticsCounterType.TrackpadEvents);
        }
    }
}
