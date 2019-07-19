// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.Graphics.OpenGL.Textures;
using osuTK.Graphics.ES30;

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

        public TextureWithRefCount(int width, int height, bool manualMipmaps = false, All filteringMode = All.Linear)
            : this(new TextureGLSingle(width, height, manualMipmaps, filteringMode))
        {
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

        // The base property references TextureGL, but doing so may throw an exception (above)
        public sealed override bool Available => base.TextureGL.Available;

        #region Disposal

        ~TextureWithRefCount()
        {
            // Finalizer implemented here rather than Texture to avoid GC overhead.
            Dispose(false);
        }

        private bool isDisposed;

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            if (isDisposed)
                return;

            isDisposed = true;

            GLWrapper.ScheduleDisposal(() => base.TextureGL.Dereference());
        }

        #endregion
    }
}
