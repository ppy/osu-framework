// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.IO.Stores;
using OpenTK.Graphics.ES30;

namespace osu.Framework.Graphics.Textures
{
    /// <summary>
    /// A texture store that bypasses atlasing and removes textures from memory after dereferenced by all consumers.
    /// </summary>
    public class LargeTextureStore : TextureStore
    {
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
            var baseTex = base.Get(name);

            if (baseTex?.TextureGL == null) return null;

            // encapsulate texture for ref counting
            return new TextureWithRefCount(baseTex.TextureGL) { ScaleAdjust = ScaleAdjust };
        }
    }
}
