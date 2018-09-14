// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using OpenTK.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.MathUtils;
using osu.Framework.Timing;
using OpenTK;

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
        private double displayFrameTime;

        private const int updates_per_second = 10;

        protected override void LoadComplete()
        {
            base.LoadComplete();

            double lastUpdate = 0;

            Scheduler.AddDelayed(() =>
            {
                if (!Counting) return;

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

                displayFps = Interpolation.Damp(displayFps, clock.FramesPerSecond, 0.01, dampRate);
                displayFrameTime = Interpolation.Damp(displayFrameTime, clock.ElapsedFrameTime - clock.SleptTime, 0.01, dampRate);

                lastUpdate = clock.CurrentTime;

                counter.Text = $"{displayFps:0}fps({displayFrameTime:0.00}ms)"
                               + $"{(clock.MaximumUpdateHz < 10000 ? clock.MaximumUpdateHz.ToString("0") : "∞").PadLeft(4)}hz";
            }, 1000.0 / updates_per_second, true);
        }

        private class CounterText : SpriteText
        {
            public CounterText()
            {
                FixedWidth = true;
            }

            protected override bool UseFixedWidthForCharacter(char c)
            {
                switch (c)
                {
                    case ',':
                    case '.':
                    case ' ':
                        return false;
                }

                return true;
            }
        }
    }
}
