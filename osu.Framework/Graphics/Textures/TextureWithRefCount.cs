// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        #region Disposal

        ~TextureWithRefCount()
        {
            // Finalizer implemented here rather than Texture to avoid GC overhead.
            Dispose(false);
        }

        private readonly List<StackTrace> stackTraces = new List<StackTrace>();

        protected override void Dispose(bool isDisposing)
        {
            if (IsDisposed)
                return;

            base.Dispose(isDisposing);

            base.TextureGL.Dereference();
            if (isDisposing) GC.SuppressFinalize(this);
        }

        #endregion
    }
}
