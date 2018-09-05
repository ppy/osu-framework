// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Graphics.OpenGL.Textures;

namespace osu.Framework.Graphics.Textures
{
    /// <summary>
    /// A texture which updates the reference count of the underlying <see cref="TextureGL"/> on ctor and disposal.
    /// </summary>
    public class TextureWithRefCount : Texture
    {
        public TextureWithRefCount(TextureGL textureGl)
            : base(textureGl)
        {
            TextureGL.Reference();
        }

        #region Disposal

        ~TextureWithRefCount()
        {
            // Finalizer implemented here rather than Texture to avoid GC overhead.
            Dispose(false);
        }

        protected override void Dispose(bool isDisposing)
        {
            if (IsDisposed)
                throw new ObjectDisposedException($"{nameof(TextureWithRefCount)} should never be disposed more than once");

            base.Dispose(isDisposing);

            TextureGL?.Dereference();
            if (isDisposing) GC.SuppressFinalize(this);
        }

        #endregion
    }
}
