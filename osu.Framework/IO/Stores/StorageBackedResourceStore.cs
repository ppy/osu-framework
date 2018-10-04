// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.IO;
using System.Threading.Tasks;
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
                if (stream == null) return null;

                byte[] buffer = new byte[stream.Length];
                stream.Read(buffer, 0, buffer.Length);
                return buffer;
            }
        }

        public virtual async Task<byte[]> GetAsync(string name)
        {
            using (Stream stream = storage.GetStream(name))
            {
                if (stream == null) return null;

                byte[] buffer = new byte[stream.Length];
                await stream.ReadAsync(buffer, 0, buffer.Length);
                return buffer;
            }
        }

        public Stream GetStream(string name) => storage.GetStream(name);

        #region IDisposable Support

        private bool isDisposed;

        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                isDisposed = true;
            }
        }

        ~StorageBackedResourceStore()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
