// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Threading;
using osu.Framework.Graphics.OpenGL.Textures;

namespace osu.Framework.Graphics.Textures
{
    /// <summary>
    /// A texture which updates the reference count of the underlying <see cref="TextureGL"/> on ctor and disposal.
    /// </summary>
    internal class TextureWithRefCount : Texture
    {
        private readonly ReferenceCount count;

        public TextureWithRefCount(TextureGL textureGl, ReferenceCount count)
            : base(textureGl)
        {
            this.count = count;

            count.Increment();
        }

        public sealed override TextureGL TextureGL
        {
            get
            {
                if (!Available)
                    throw new InvalidOperationException($"Attempting to access a {nameof(TextureWithRefCount)}'s underlying texture after all references are lost.");

                return base.TextureGL;
            }
        }

        // The base property invokes the overridden TextureGL property, which will throw an exception if not available
        // So this property is redirected to reference the intended member
        public sealed override bool Available => base.TextureGL.Available;

        ~TextureWithRefCount()
        {
            // Finalizer implemented here rather than Texture to avoid GC overhead.
            Dispose(false);
        }

        public bool IsDisposed { get; private set; }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            if (IsDisposed)
                return;

            IsDisposed = true;

            count.Decrement();
        }

        public class ReferenceCount
        {
            private readonly object lockObject;
            private readonly Action onAllReferencesLost;

            private int referenceCount;

            public ReferenceCount(object lockObject, Action onAllReferencesLost)
            {
                this.lockObject = lockObject;
                this.onAllReferencesLost = onAllReferencesLost;
            }

            public void Increment()
            {
                lock (lockObject)
                    Interlocked.Increment(ref referenceCount);
            }

            public void Decrement()
            {
                lock (lockObject)
                {
                    if (Interlocked.Decrement(ref referenceCount) == 0)
                        onAllReferencesLost?.Invoke();
                }
            }
        }
    }
}
