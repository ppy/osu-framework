// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
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
            this.LogIfNonBackgroundThread(name);

            using (Stream stream = storage.GetStream(name))
            {
                if (stream == null) return null;

                byte[] buffer = new byte[stream.Length];
                stream.Read(buffer, 0, buffer.Length);
                return buffer;
            }
        }

        public virtual async Task<byte[]> GetAsync(string name, CancellationToken cancellationToken = default)
        {
            this.LogIfNonBackgroundThread(name);

            using (Stream stream = storage.GetStream(name))
            {
                if (stream == null) return null;

                byte[] buffer = new byte[stream.Length];
                await stream.ReadAsync(buffer.AsMemory(), cancellationToken).ConfigureAwait(false);
                return buffer;
            }
        }

        public Stream GetStream(string name)
        {
            this.LogIfNonBackgroundThread(name);

            return storage.GetStream(name);
        }

        public IEnumerable<string> GetAvailableResources() =>
            storage.GetDirectories(string.Empty).SelectMany(d => storage.GetFiles(d)).ExcludeSystemFileNames();

        #region IDisposable Support

        public void Dispose()
        {
        }

        #endregion
    }
}
