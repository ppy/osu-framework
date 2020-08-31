// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using osu.Framework.IO.Stores;
using osuTK.Graphics.ES30;
using osu.Framework.Graphics.OpenGL.Textures;

namespace osu.Framework.Graphics.Textures
{
    /// <summary>
    /// A texture store that bypasses atlasing and removes textures from memory after dereferenced by all consumers.
    /// </summary>
    public class LargeTextureStore : TextureStore
    {
        private readonly object referenceCountLock = new object();
        private readonly Dictionary<string, TextureWithRefCount.ReferenceCount> referenceCounts = new Dictionary<string, TextureWithRefCount.ReferenceCount>();

        public LargeTextureStore(IResourceStore<TextureUpload> store = null, All filteringMode = All.Linear)
            : base(store, false, filteringMode, true)
        {
        }

        /// <summary>
        /// Retrieves a texture.
        /// This texture should only be assigned once, as reference counting is being used internally.
        /// If you wish to use the same texture multiple times, call this method an equal number of times.
        /// </summary>
        /// <param name="name">The name of the texture.</param>
        /// <param name="wrapModeS">The horizontal wrap mode of the texture.</param>
        /// <param name="wrapModeT">The vertical wrap mode of the texture.</param>
        /// <returns>The texture.</returns>
        public override Texture Get(string name, WrapMode wrapModeS, WrapMode wrapModeT)
        {
            lock (referenceCountLock)
            {
                var tex = base.Get(name, wrapModeS, wrapModeT);

                if (tex?.TextureGL == null)
                    return null;

                if (!referenceCounts.TryGetValue(tex.LookupKey, out TextureWithRefCount.ReferenceCount count))
                    referenceCounts[tex.LookupKey] = count = new TextureWithRefCount.ReferenceCount(referenceCountLock, () => onAllReferencesLost(tex));

                return new TextureWithRefCount(tex.TextureGL, count);
            }
        }

        private void onAllReferencesLost(Texture texture)
        {
            Debug.Assert(Monitor.IsEntered(referenceCountLock));

            referenceCounts.Remove(texture.LookupKey);
            Purge(texture);
        }
    }
}
