// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.Textures;

namespace osu.Framework.Graphics.Sprites
{
    public class Box : Sprite
    {
        protected override void Load(BaseGame game)
        {
            base.Load(game);

            Texture = Texture.WhitePixel;
        }
    }
}
