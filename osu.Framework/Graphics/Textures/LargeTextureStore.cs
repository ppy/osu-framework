// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Concurrent;
using System.Threading.Tasks;
using osu.Framework.IO.Stores;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics.Textures
{
    /// <summary>
    /// A texture store that bypasses atlasing and removes textures from memory after dereferenced by all consumers.
    /// </summary>
    public class LargeTextureStore : TextureStore
    {
        private readonly ConcurrentDictionary<string, TextureWithRefCount.ReferenceCount> referenceCounts = new ConcurrentDictionary<string, TextureWithRefCount.ReferenceCount>();

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
            var tex = base.Get(name);

            if (tex?.TextureGL == null)
                return null;

            var count = referenceCounts.GetOrAdd(name, n => new TextureWithRefCount.ReferenceCount(() => onAllReferencesLost(name)));

            return new TextureWithRefCount(tex.TextureGL, count);
        }

        private void onAllReferencesLost(string name)
        {
            referenceCounts.TryRemove(name, out _);
            Purge(name);
        }
    }
}
