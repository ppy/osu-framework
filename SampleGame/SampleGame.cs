// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework;
using osu.Framework.Graphics;
using OpenTK;
using OpenTK.Graphics;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Allocation;

namespace SampleGame
{
    internal class SampleGame : Game
    {
        private Box box;

        [BackgroundDependencyLoader]
        private void load()
        {
            Add(box = new Box
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Size = new Vector2(150, 150),
                Colour = Color4.Tomato
            });
        }

        protected override void Update()
        {
            base.Update();
            box.Rotation += (float)Time.Elapsed / 10;
        }
    }
}
