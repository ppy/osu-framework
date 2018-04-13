// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.IO.Stores;

namespace osu.Framework.Graphics.Textures
{
    public class PrefixTextureStore : TextureStore
    {
        private readonly string prefix;

        public PrefixTextureStore(string prefix, IResourceStore<RawTexture> stores)
            : base(stores)
        {
            this.prefix = prefix;
        }

        public override Texture Get(string name)
        {
            return base.Get($@"{prefix}-{name}");
        }
    }
}
