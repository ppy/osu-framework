// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Threading;
using osu.Framework.Graphics.Rendering;

namespace osu.Framework.Graphics.Textures
{
    /// <summary>
    /// A texture which shares a common reference count with all other textures using the same <see cref="NativeTexture"/>.
    /// </summary>
    internal class TextureWithRefCount : Texture
    {
        private readonly ReferenceCount count;

        public TextureWithRefCount(Texture parent, ReferenceCount count)
            : base(parent)
        {
            this.count = count;

            count.Increment();
        }

        internal sealed override INativeTexture NativeTexture
        {
            get
            {
                if (!Available)
                    throw new InvalidOperationException($"Attempting to access a {nameof(TextureWithRefCount)}'s underlying texture after all references are lost.");

                return base.NativeTexture;
            }
        }

        // The base property invokes the overridden NativeTexture property, which will throw an exception if not available
        // So this property is redirected to reference the intended member
        public sealed override bool Available => base.NativeTexture.Available;

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
            private readonly Action? onAllReferencesLost;

            private int referenceCount;

            /// <summary>
            /// Creates a new <see cref="ReferenceCount"/>.
            /// </summary>
            /// <param name="lockObject">The <see cref="object"/> which locks will be taken out on.</param>
            /// <param name="onAllReferencesLost">A delegate to invoke after all references have been lost.</param>
            public ReferenceCount(object lockObject, Action onAllReferencesLost)
            {
                this.lockObject = lockObject;
                this.onAllReferencesLost = onAllReferencesLost;
            }

            /// <summary>
            /// Increments the reference count.
            /// </summary>
            public void Increment()
            {
                lock (lockObject)
                    Interlocked.Increment(ref referenceCount);
            }

            /// <summary>
            /// Decrements the reference count, invoking <see cref="onAllReferencesLost"/> if there are no remaining references.
            /// The delegate is invoked while a lock on the provided <see cref="lockObject"/> is held.
            /// </summary>
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
