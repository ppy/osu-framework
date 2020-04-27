// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osuTK.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Statistics;
using osu.Framework.Utils;
using osu.Framework.Threading;
using osu.Framework.Timing;
using osuTK;

namespace osu.Framework.Graphics.Performance
{
    internal class FrameTimeDisplay : Container
    {
        private readonly SpriteText counter;

        private readonly ThrottledFrameClock clock;
        private readonly GameThread thread;

        public bool Counting = true;

        public FrameTimeDisplay(ThrottledFrameClock clock, GameThread thread)
        {
            this.clock = clock;
            this.thread = thread;

            Masking = true;
            CornerRadius = 5;

            AddRange(new Drawable[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = Color4.Black,
                    Alpha = 0.75f
                },
                counter = new CounterText
                {
                    Anchor = Anchor.TopRight,
                    Origin = Anchor.TopRight,
                    Spacing = new Vector2(-1, 0),
                    Text = @"...",
                }
            });
        }

        private float aimWidth;

        private double displayFps;

        private double rollingElapsed;

        private int framesSinceLastUpdate;

        private double elapsedSinceLastUpdate;

        private const int updates_per_second = 10;

        protected override void LoadComplete()
        {
            base.LoadComplete();

            double lastUpdate = 0;

            thread.Scheduler.AddDelayed(() =>
            {
                if (!Counting) return;

                double clockFps = clock.FramesPerSecond;
                double updateHz = clock.MaximumUpdateHz;

                Schedule(() =>
                {
                    if (!Precision.AlmostEquals(counter.DrawWidth, aimWidth))
                    {
                        ClearTransforms();

                        if (aimWidth == 0)
                            Size = counter.DrawSize;
                        else if (Precision.AlmostBigger(counter.DrawWidth, aimWidth))
                            this.ResizeTo(counter.DrawSize, 200, Easing.InOutSine);
                        else
                            this.Delay(1500).ResizeTo(counter.DrawSize, 500, Easing.InOutSine);

                        aimWidth = counter.DrawWidth;
                    }

                    double dampRate = Math.Max(Clock.CurrentTime - lastUpdate, 0) / 1000;

                    displayFps = Interpolation.Damp(displayFps, clockFps, 0.01, dampRate);
                    if (framesSinceLastUpdate > 0)
                        rollingElapsed = Interpolation.Damp(rollingElapsed, elapsedSinceLastUpdate / framesSinceLastUpdate, 0.01, dampRate);

                    lastUpdate = Clock.CurrentTime;

                    framesSinceLastUpdate = 0;
                    elapsedSinceLastUpdate = 0;

                    counter.Text = $"{displayFps:0}fps({rollingElapsed:0.00}ms)"
                                   + (clock.Throttling ? $"{(updateHz < 10000 ? updateHz.ToString("0") : "∞").PadLeft(4)}hz" : string.Empty);
                });
            }, 1000.0 / updates_per_second, true);
        }

        private class CounterText : SpriteText
        {
            public CounterText()
            {
                Font = new FontUsage(fixedWidth: true);
            }

            protected override char[] FixedWidthExcludeCharacters { get; } = { ',', '.', ' ' };
        }

        public void NewFrame(FrameStatistics frame)
        {
            if (!Counting) return;

            foreach (var pair in frame.CollectedTimes)
            {
                if (pair.Key != PerformanceCollectionType.Sleep)
                    elapsedSinceLastUpdate += pair.Value;
            }

            framesSinceLastUpdate++;
        }
    }
}
