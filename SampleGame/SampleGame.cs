// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Drawables;
using OpenTK;
using OpenTK.Graphics;

namespace SampleGame
{
    class SampleGame : Game
    {
        public override void Load()
        {
            base.Load();

            Box box;

            Add(box = new Box
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Size = new Vector2(150, 150),
                Colour = Color4.Tomato
            });

            box.RotateTo(360 * 10, 60000);
        }
    }
}
