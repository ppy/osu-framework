// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Statistics;
using osu.Framework.Timing;
using osu.Framework.Utils;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Graphics.Performance
{
    internal class FrameTimeDisplay : Container
    {
        private readonly SpriteText counter;

        private readonly ThrottledFrameClock clock;

        public bool Counting = true;

        public FrameTimeDisplay(ThrottledFrameClock clock)
        {
            this.clock = clock;

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
        private double lastUpdateLocalTime;
        private double lastFrameFramesPerSecond;

        private const int updates_per_second = 10;

        protected override void Update()
        {
            base.Update();

            if (!Precision.AlmostEquals(counter.DrawWidth, aimWidth))
            {
                ClearTransforms();

                if (aimWidth == 0)
                    Size = counter.DrawSize;
                else if (Precision.AlmostBigger(counter.DrawWidth, aimWidth))
                    this.ResizeTo(counter.DrawSize, 200, Easing.OutQuint);
                else
                    this.Delay(500).ResizeTo(counter.DrawSize, 200, Easing.InOutSine);

                aimWidth = counter.DrawWidth;
            }

            if (Clock.CurrentTime - lastUpdateLocalTime > 1000.0 / updates_per_second)
                updateDisplay();
        }

        private void updateDisplay()
        {
            double dampRate = Math.Max(Clock.CurrentTime - lastUpdateLocalTime, 0) / 1000;

            displayFps = Interpolation.Damp(displayFps, lastFrameFramesPerSecond, 0.01, dampRate);

            if (framesSinceLastUpdate > 0)
            {
                rollingElapsed = Interpolation.Damp(rollingElapsed, elapsedSinceLastUpdate / framesSinceLastUpdate, 0.01, dampRate);
            }

            lastUpdateLocalTime = Clock.CurrentTime;

            framesSinceLastUpdate = 0;
            elapsedSinceLastUpdate = 0;

            counter.Text = $"{displayFps:0}fps ({rollingElapsed:0.00}ms)"
                           + (clock.Throttling ? $"{(clock.MaximumUpdateHz > 0 && clock.MaximumUpdateHz < 10000 ? clock.MaximumUpdateHz.ToString("0") : "∞"),4}hz" : string.Empty);
        }

        private class CounterText : SpriteText
        {
            public CounterText()
            {
                Font = FrameworkFont.Regular.With(fixedWidth: true);
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
            lastFrameFramesPerSecond = frame.FramesPerSecond;
        }
    }
}
