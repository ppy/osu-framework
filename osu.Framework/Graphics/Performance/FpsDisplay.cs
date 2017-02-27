﻿// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Transforms;
using osu.Framework.MathUtils;
using osu.Framework.Timing;

namespace osu.Framework.Graphics.Performance
{
    class FpsDisplay : Container
    {
        private readonly SpriteText counter;

        private readonly IFrameBasedClock clock;
        private double displayFps;

        public bool Counting = true;

        public FpsDisplay(IFrameBasedClock clock)
        {
            this.clock = clock;

            Masking = true;
            CornerRadius = 5;

            Add(new Drawable[]
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
                    Text = @"...",
                    FixedWidth = true,
                }
            });
        }

        private float aimWidth;

        protected override void Update()
        {
            base.Update();

            if (!Counting) return;

            displayFps = Interpolation.Damp(displayFps, clock.FramesPerSecond, 0.01, clock.ElapsedFrameTime / 1000);

            if (counter.DrawWidth != aimWidth)
            {
                ClearTransforms();

                if (aimWidth == 0)
                    Size = counter.DrawSize;
                else if (Precision.AlmostBigger(counter.DrawWidth, aimWidth))
                    ResizeTo(counter.DrawSize, 200, EasingTypes.InOutSine);
                else
                {
                    Delay(1500);
                    ResizeTo(counter.DrawSize, 500, EasingTypes.InOutSine);
                }

                aimWidth = counter.DrawWidth;
            }

            counter.Text = displayFps.ToString(@"0");
        }
    }
}
