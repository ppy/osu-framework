// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

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
            textureGl.Reference();
        }

        internal int ReferenceCount => base.TextureGL.ReferenceCount;

        public sealed override TextureGL TextureGL
        {
            get
            {
                var tex = base.TextureGL;
                if (tex.ReferenceCount <= 0)
                    throw new InvalidOperationException($"Attempting to access a {nameof(TextureWithRefCount)}'s underlying texture after all references are lost.");
                return tex;
            }
        }

        // Can't reference our own TextureGL here as an exception may be thrown
        public sealed override bool Available => !isDisposed && !base.TextureGL.IsDisposed;
        private bool isDisposed;

        #region Disposal

        ~TextureWithRefCount()
        {
            // Finalizer implemented here rather than Texture to avoid GC overhead.
            Dispose(false);
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            if (isDisposed)
                return;
            isDisposed = true;

            base.TextureGL.Dereference();
            if (isDisposing) GC.SuppressFinalize(this);
        }

        #endregion
    }
}
