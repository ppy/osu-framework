// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.OpenGL.Textures;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics.Textures
{
    public class DisposableTexture : Texture
    {
        public DisposableTexture(TextureGL textureGl)
            : base(textureGl)
        {
        }

        public DisposableTexture(int width, int height, bool manualMipmaps = false, All filteringMode = All.Linear)
            : base(width, height, manualMipmaps, filteringMode)
        {
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);
            TextureGL.Dispose();
        }
    }
}
