// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Drawables;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Timing;

namespace osu.Framework.Graphics.Performance
{
    class FpsDisplay : AutoSizeContainer
    {
        SpriteText counter;

        private IFrameBasedClock clock;
        private double AverageFrameTime;

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
                SizeMode = InheritMode.XY,
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

            // Accumulate a sliding average over frame time and frames per second.
            double alpha = 0.01;
            AverageFrameTime = AverageFrameTime == 0 ? clock.AverageFrameTime : (AverageFrameTime * (1 - alpha) + clock.AverageFrameTime * alpha);

            counter.Text = (1000 / AverageFrameTime).ToString(@"0");
        }
    }
}
