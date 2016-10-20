// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE


namespace osu.Framework.Graphics.Sprites
{
    public class Box : Sprite
    {
        public override void Load(BaseGame game)
        {
            base.Load(game);

            Texture = game.Textures.Get(@"_whitepixel");
        }
    }
}
