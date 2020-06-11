// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Primitives;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics.OpenGL.Textures
{
    /// <summary>
    /// A special texture which refers to the area of a texture atlas which is white.
    /// Allows use of such areas while being unaware of whether we need to bind a texture or not.
    /// </summary>
    internal class TextureGLAtlasWhite : TextureGLSub
    {
        public TextureGLAtlasWhite(TextureGLSingle parent)
            : base(new RectangleI(0, 0, 1, 1), parent)
        {
            Opacity = Opacity.Opaque;
        }

        internal override bool Bind(TextureUnit unit, WrapMode wrapModeS, WrapMode wrapModeT)
        {
            //we can use the special white space from any atlas texture.
            if (GLWrapper.AtlasTextureIsBound(unit))
                return true;

            return base.Bind(unit, wrapModeS, wrapModeT);
        }
    }
}
