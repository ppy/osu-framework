// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Rendering;

namespace osu.Framework.Graphics.Textures
{
    /// <summary>
    /// A texture which can cleans up any resources held by the underlying <see cref="INativeTexture"/> on <see cref="Dispose"/>.
    /// </summary>
    public class DisposableTexture : Texture
    {
        private readonly Texture parent;

        public DisposableTexture(Texture parent)
            : base(parent)
        {
            this.parent = parent;
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);
            NativeTexture.Dispose();
            parent.Dispose();
        }
    }
}
