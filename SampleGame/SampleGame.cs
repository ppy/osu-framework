// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework;
using osu.Framework.Graphics;
using OpenTK;
using OpenTK.Graphics;
using osu.Framework.Graphics.Sprites;

namespace SampleGame
{
    class SampleGame : BaseGame
    {
        private Box box;

        protected override void Load(BaseGame game)
        {
            base.Load(game);

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
            box.Rotation += (float)Clock.ElapsedFrameTime / 10;
        }
    }
}
