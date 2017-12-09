// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Diagnostics;
using System.IO;
using System.Drawing;
using osu.Framework.IO.Stores;

namespace osu.Framework.Graphics.Textures
{
    public class RawTextureLoaderStore : ResourceStore<RawTexture>
    {
        private IResourceStore<byte[]> store { get; }

        public RawTextureLoaderStore(IResourceStore<byte[]> store)
        {
            this.store = store;
            (store as ResourceStore<byte[]>)?.AddExtension(@"png");
            (store as ResourceStore<byte[]>)?.AddExtension(@"jpg");
        }

        public override RawTexture Get(string name)
        {
            try
            {
                using (var stream = store.GetStream(name))
                {
                    if (stream == null) return null;

                    return RawTexture.FromStream(stream);
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
