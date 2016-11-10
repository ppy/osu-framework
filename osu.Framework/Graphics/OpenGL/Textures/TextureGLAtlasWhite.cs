// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Drawing;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.Graphics.OpenGL.Textures;

namespace osu.Framework.Graphics.OpenGL.Textures
{
    /// <summary>
    /// A special texture which refers to the area of a texture atlas which is white.
    /// Allows use of such areas while being unaware of whether we need to bind a texture or not.
    /// </summary>
    class TextureGLAtlasWhite : TextureGLSub
    {
        public TextureGLAtlasWhite(TextureGLSingle parent) : base(new Rectangle(0, 0, 1, 1), parent)
        {
        }

        public override bool Bind()
        {
            //we can use the special white space from any atlas texture.
            if (GLWrapper.AtlasTextureIsBound)
                return true;

            return base.Bind();
        }
    }
}