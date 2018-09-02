// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Graphics.OpenGL.Textures;

namespace osu.Framework.Graphics.Textures
{
    /// <summary>
    /// A texture which updates the reference count of the underlying <see cref="TextureGL"/> on ctor and disposal.
    /// </summary>
    public class TextureWithRefCount : Texture, IDisposable
    {
        public TextureWithRefCount(TextureGL textureGl)
            : base(textureGl)
        {
            TextureGL.Reference();
        }

        #region Disposal

        public bool IsDisposed { get; private set; }

        ~TextureWithRefCount()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool isDisposing)
        {
            if (IsDisposed)
                return;
            IsDisposed = true;
            TextureGL?.Dereference();
        }

        #endregion
    }
}
