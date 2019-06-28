// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.OpenGL.Textures;
using osu.Framework.Graphics.Primitives;

namespace osu.Framework.Graphics.Textures
{
    internal class TextureWhitePixel : Texture
    {
        private readonly int atlasTextureSize;

        public TextureWhitePixel(TextureGL textureGl, int atlasTextureSize)
            : base(textureGl)
        {
            this.atlasTextureSize = atlasTextureSize;
        }

        protected override RectangleF TextureBounds(RectangleF? textureRect = null)
        {
            // We need non-zero texture bounds for EdgeSmoothness to work correctly.
            // Let's be very conservative and use a tenth of the size of a pixel in the
            // largest possible texture.
            float smallestPixelTenth = 0.1f / atlasTextureSize;
            return new RectangleF(0, 0, smallestPixelTenth, smallestPixelTenth);
        }
    }
}
