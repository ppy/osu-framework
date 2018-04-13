// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.OpenGL;
using osu.Framework.Graphics.OpenGL.Textures;
using osu.Framework.Graphics.Primitives;
using System;

namespace osu.Framework.Graphics.Textures
{
    internal class TextureWhitePixel : Texture
    {
        public TextureWhitePixel(TextureGL textureGl)
            : base(textureGl)
        {
        }

        protected override void Dispose(bool isDisposing)
        {
            if (isDisposing)
                throw new InvalidOperationException($"May not dispose {nameof(TextureWhitePixel)} explicitly.");
            base.Dispose(false);
        }

        protected override RectangleF TextureBounds(RectangleF? textureRect = null)
        {
            // We need non-zero texture bounds for EdgeSmoothness to work correctly.
            // Let's be very conservative and use a tenth of the size of a pixel in the
            // largest possible texture.
            float smallestPixelTenth = 0.1f / GLWrapper.MaxTextureSize;
            return new RectangleF(0, 0, smallestPixelTenth, smallestPixelTenth);
        }
    }
}
