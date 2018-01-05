// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.IO;
using osu.Framework.Platform;

namespace osu.Framework.IO.Stores
{
    /// <summary>
    /// A resource store that uses an underlying <see cref="Storage"/> backing.
    /// </summary>
    public class StorageBackedResourceStore : IResourceStore<byte[]>
    {
        private readonly Storage storage;

        public StorageBackedResourceStore(Storage storage)
        {
            this.storage = storage;
        }

        public byte[] Get(string name)
        {
            using (Stream stream = storage.GetStream(name))
            {
                byte[] buffer = new byte[stream.Length];
                stream.Read(buffer, 0, buffer.Length);
                return buffer;
            }
        }

        public Stream GetStream(string name)
        {
            return storage.GetStream(name);
        }
    }
}
