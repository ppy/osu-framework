// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
using OpenTK.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Drawables;
using osu.Framework.Graphics.Sprites;
using osu.Framework.MathUtils;
using osu.Framework.Timing;
using System;

namespace osu.Framework.Graphics.Performance
{
    class FpsDisplay : AutoSizeContainer
    {
        SpriteText counter;

        private IFrameBasedClock clock;
        private double displayFPS;

        public bool Counting = true;

        public FpsDisplay(IFrameBasedClock clock)
        {
            this.clock = clock;
        }

        public override void Load()
        {
            base.Load();

            Add(new Box
            {
                RelativeCoords = Axis.Both,
                Colour = Color4.Black,
                Alpha = 0.2f
            });

            Add(counter = new SpriteText
            {
                Text = @"...",
                FixedWidth = true,
            });
        }

        protected override void Update()
        {
            base.Update();

            if (!Counting) return;

            displayFPS = Interpolation.Damp(displayFPS, clock.FramesPerSecond, 0.01, Clock.ElapsedFrameTime / 1000);

            counter.Text = displayFPS.ToString(@"0");
        }
    }
}
