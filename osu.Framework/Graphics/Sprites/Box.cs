// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.OpenGL.Textures;
using osu.Framework.Graphics.Textures;

namespace osu.Framework.Graphics.Sprites
{
    public class Box : Sprite
    {
        private static Texture whitePixel;

        public Box()
        {
            if (whitePixel == null)
            {
                whitePixel = new Texture(1, 1, true);
                whitePixel.SetData(new TextureUpload(new byte[] { 255, 255, 255, 255 }));
            }

            Texture = whitePixel;
        }
    }
}
