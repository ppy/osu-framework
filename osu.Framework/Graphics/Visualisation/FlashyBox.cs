// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
using OpenTK.Graphics;
using osu.Framework.Graphics.Sprites;

namespace osu.Framework.Graphics.Visualisation
{
    class FlashyBox : Box
    {
        public FlashyBox()
        {
            Size = new Vector2(4);
            Origin = Anchor.Centre;
            Colour = Color4.Red;
        }

        public override void Load(BaseGame game)
        {
            base.Load(game);

            FadeColour(Color4.White, 500);
            Delay(500);
            FadeColour(Color4.Red, 500);
            Delay(500);
            Loop();

            DelayReset();
            ScaleTo(3);
            ScaleTo(1, 200);
        }
    }
}