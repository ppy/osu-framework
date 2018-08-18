// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Globalization;
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
                counter = new SpriteText
                {
                    Anchor = Anchor.TopRight,
                    Origin = Anchor.TopRight,
                    Spacing = new Vector2(-1, 0),
                    Text = @"...",
                    FixedWidth = true,
                }
            });
        }

        private float aimWidth;

        private double displayFps;

        protected override void Update()
        {
            base.Update();

            if (!Counting) return;

            if (counter.DrawWidth != aimWidth)
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

            displayFps = Interpolation.Damp(displayFps, clock.FramesPerSecond, 0.01, Math.Max(Clock.ElapsedFrameTime, 0) / 1000);

            counter.Text = $"{displayFps:0}fps({clock.AverageFrameTime:0.00}ms)"
                           + $"{(clock.MaximumUpdateHz < 10000 ? clock.MaximumUpdateHz.ToString(CultureInfo.InvariantCulture) : "∞").PadLeft(4)}hz";
        }
    }
}
