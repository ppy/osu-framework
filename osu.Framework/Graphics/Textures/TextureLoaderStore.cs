// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Threading.Tasks;
using osu.Framework.IO.Stores;

namespace osu.Framework.Graphics.Textures
{
    public class TextureLoaderStore : ResourceStore<TextureUpload>
    {
        private IResourceStore<byte[]> store { get; }

        public TextureLoaderStore(IResourceStore<byte[]> store)
        {
            this.store = store;
            (store as ResourceStore<byte[]>)?.AddExtension(@"png");
            (store as ResourceStore<byte[]>)?.AddExtension(@"jpg");
        }

        public override Task<TextureUpload> GetAsync(string name) => Task.Run(() => Get(name));

        public override TextureUpload Get(string name)
        {
            try
            {
                using (var stream = store.GetStream(name))
                {
                    if (stream != null)
                        return new TextureUpload(stream);
                }
            }
            catch
            {
            }

            return null;
        }
    }
}
