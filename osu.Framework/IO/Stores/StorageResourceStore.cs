// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.IO;
using osu.Framework.Platform;

namespace osu.Framework.IO.Stores
{
    public class StorageResourceStore : IResourceStore<byte[]>
    {
        private Storage storage;

        public StorageResourceStore(Storage storage)
        {
            this.storage = storage;
        }

        public byte[] Get(string name)
        {
            Stream stream = storage.GetStream(name);

            using (MemoryStream ms = new MemoryStream())
            {
                stream.CopyTo(ms);
                return ms.ToArray();
            }
        }

        public Stream GetStream(string name) => storage.GetStream(name);
    }
}
