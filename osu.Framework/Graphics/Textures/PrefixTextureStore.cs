// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.IO.Stores;

namespace osu.Framework.Graphics.Textures
{
    public class PrefixTextureStore : TextureStore
    {
        private readonly string prefix;

        public PrefixTextureStore(string prefix, IResourceStore<TextureUpload> stores)
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
