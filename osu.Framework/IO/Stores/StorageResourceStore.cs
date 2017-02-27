using System;
using System.IO;
using osu.Framework.IO.Stores;
using osu.Framework.Platform;

namespace osu.Framework
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
