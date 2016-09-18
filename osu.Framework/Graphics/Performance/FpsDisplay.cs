//Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
//Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Timing;
using OpenTK;

namespace osu.Framework.Graphics.Performance
{
    class FpsDisplay : AutoSizeContainer
    {
        SpriteText counter;

        private readonly string name;
        private ThrottledFrameClock clock;

        public FpsDisplay(string name, ThrottledFrameClock clock)
        {
            this.name = name;
            this.clock = clock;
        }

        public override void Load()
        {
            base.Load();

            Add(counter = new SpriteText()
            {
                Text = @"...",
                FixedWidth = true,
            });
        }

        protected override void Update()
        {
            base.Update();

            counter.Text = $@"{name}" + (1000 / clock.AverageFrameTime).ToString(@"0").PadLeft(4);
        }
    }
}
