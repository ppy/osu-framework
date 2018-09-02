// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.IO.Stores;

namespace osu.Framework.Graphics.Textures
{
    /// <summary>
    /// A texture store that bypasses atlasing and removes textures from memory after dereferenced by all consumers.
    /// </summary>
    public class LargeTextureStore : TextureStore
    {
        public LargeTextureStore(IResourceStore<RawTexture> store = null)
            : base(store, false)
        {
        }

        public override Texture Get(string name)
        {
            var baseTex = base.Get(name);

            if (baseTex?.TextureGL == null) return null;

            // encapsulate texture for ref counting
            return new TextureWithRefCount(baseTex.TextureGL) { ScaleAdjust = ScaleAdjust };
        }
    }
}
