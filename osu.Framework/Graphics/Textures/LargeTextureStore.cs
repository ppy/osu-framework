// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using osu.Framework.IO.Stores;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics.Textures
{
    /// <summary>
    /// A texture store that bypasses atlasing and removes textures from memory after dereferenced by all consumers.
    /// </summary>
    public class LargeTextureStore : TextureStore
    {
        private readonly object referenceCountLock = new object();
        private readonly Dictionary<string, TextureWithRefCount.ReferenceCount> referenceCounts = new Dictionary<string, TextureWithRefCount.ReferenceCount>();

        public LargeTextureStore(IResourceStore<TextureUpload> store = null)
            : base(store, false, All.Linear, true)
        {
        }

        /// <summary>
        /// Retrieves a texture.
        /// This texture should only be assigned once, as reference counting is being used internally.
        /// If you wish to use the same texture multiple times, call this method an equal number of times.
        /// </summary>
        /// <param name="name">The name of the texture.</param>
        /// <returns>The texture.</returns>
        public override Texture Get(string name)
        {
            lock (referenceCountLock)
            {
                var tex = base.Get(name);

                if (tex?.TextureGL == null)
                    return null;

                if (!referenceCounts.TryGetValue(name, out TextureWithRefCount.ReferenceCount count))
                    referenceCounts[name] = count = new TextureWithRefCount.ReferenceCount(referenceCountLock, () => onAllReferencesLost(name));

                return new TextureWithRefCount(tex.TextureGL, count);
            }
        }

        private void onAllReferencesLost(string name)
        {
            Debug.Assert(Monitor.IsEntered(referenceCountLock));

            referenceCounts.Remove(name);
            Purge(name);
        }
    }
}
