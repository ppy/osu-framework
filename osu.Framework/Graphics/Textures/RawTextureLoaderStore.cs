// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.IO.Stores;

namespace osu.Framework.Graphics.Textures
{
    public class RawTextureLoaderStore : ResourceStore<RawTextureUnknownStream>
    {
        private IResourceStore<byte[]> store { get; }

        public RawTextureLoaderStore(IResourceStore<byte[]> store)
        {
            this.store = store;
            (store as ResourceStore<byte[]>)?.AddExtension(@"png");
            (store as ResourceStore<byte[]>)?.AddExtension(@"jpg");
        }

        public override RawTextureUnknownStream Get(string name)
        {
            try
            {
                using (var stream = store.GetStream(name))
                {
                    if (stream == null) return null;

                    return new RawTextureUnknownStream(stream);
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
