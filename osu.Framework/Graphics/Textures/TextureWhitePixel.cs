// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Graphics.Primitives;

namespace osu.Framework.Graphics.Textures
{
    internal class TextureWhitePixel : TextureRegion
    {
        public TextureWhitePixel(Texture texture)
            : base(texture, new RectangleI(0, 0, 1, 1), texture.WrapModeS, texture.WrapModeT)
        {
            Opacity = Opacity.Opaque;
        }

        public override RectangleF GetTextureRect(RectangleF? area = null)
        {
            // We need non-zero texture bounds for EdgeSmoothness to work correctly.
            // Let's be very conservative and use a tenth of the size of a pixel in the
            // largest possible texture.
            float smallestPixelTenth = 0.1f / NativeTexture.MaxSize;
            return new RectangleF(0, 0, smallestPixelTenth, smallestPixelTenth);
        }
    }
}
