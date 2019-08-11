// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using System.Threading.Tasks;
using osu.Framework.Extensions;
using osu.Framework.Platform;

namespace osu.Framework.IO.Stores
{
    /// <summary>
    /// An <see cref="OnlineStore"/> with file system caching
    /// </summary>
    public class CachedOnlineStore : OnlineStore
    {
        private readonly Storage cacheStorage;
        public readonly TimeSpan Duration;

        /// <summary>
        /// Constructs a <see cref="CachedOnlineStore"/>
        /// </summary>
        /// <param name="cacheStorage">A <see cref="Storage"/> which will be used as a cache</param>
        /// <param name="duration">A <see cref="TimeSpan"/> since last file access after which it will be considered expired</param>
        public CachedOnlineStore(Storage cacheStorage, TimeSpan duration)
        {
            this.cacheStorage = cacheStorage;
            Duration = duration;
        }

        /// <summary>
        /// Retrieves an object from cache if it is possible. Otherwise calls an <see cref="OnlineStore"/> implementation and caches its result."/> 
        /// </summary>
        /// <param name="url">The address of the object.</param>
        /// <returns>The object.</returns>
        public override async Task<byte[]> GetAsync(string url)
        {
            var data = getCached(url);

            if (data == null && (data = await base.GetAsync(url)) != null)
                cache(url, data);

            return data;
        }

        /// <summary>
        /// Retrieves an object from cache if it is possible. Otherwise calls an <see cref="OnlineStore"/> implementation and caches its result."/> 
        /// </summary>
        /// <param name="url">The address of the object.</param>
        /// <returns>The object.</returns>
        public override byte[] Get(string url)
        {
            var data = getCached(url);

            if (data == null && (data = base.Get(url)) != null)
                cache(url, data);

            return data;
        }

        public override Stream GetStream(string url)
        {
            var bytes = getCached(url);

            if (bytes != null)
                return new MemoryStream(bytes);

            var stream = base.GetStream(url);
            stream.Read(bytes = new byte[stream.Length], 0, bytes.Length);
            cache(url, bytes);

            return stream;
        }

        /// <summary>
        /// Writes data to the cache <see cref="Storage"/>
        /// </summary>
        /// <param name="url">The address of the object.</param>
        /// <param name="data">The object.</param>
        private void cache(string url, byte[] data)
        {
            using (var stream = cacheStorage.GetStream(url.ComputeMD5Hash(), FileAccess.Write, FileMode.Create))
                stream.Write(data, 0, data.Length);
        }

        /// <summary>
        /// Retrieves an object from cache.
        /// </summary>
        /// <param name="url">The address of the object.</param>
        /// <returns>The object or <value>null</value> if there in no suitable object in the cache.</returns>
        private byte[] getCached(string url)
        {
            byte[] data = null;
            var fileName = url.ComputeMD5Hash();

            if (cacheStorage.Exists(fileName))
            {
                using (var stream = cacheStorage.GetStream(fileName))
                    stream.Read(data = new byte[stream.Length], 0, data.Length);

                cacheStorage.SetLastAccessTime(fileName, DateTime.Now);
            }

            return data;
        }

        public int Clear() => Clear(Duration);

        /// <summary>
        /// Clears cache
        /// </summary>
        /// <param name="duration">A <see cref="TimeSpan"/> since last file access after which it will be considered expired</param>
        /// <returns>The number of removed objects</returns>
        public int Clear(TimeSpan duration)
        {
            var removed = 0;
            var cachedFiles = cacheStorage.GetFiles(string.Empty);

            foreach (var cachedFile in cachedFiles)
                if (DateTime.Now - cacheStorage.GetLastAccessTime(cachedFile) > duration)
                {
                    cacheStorage.Delete(cachedFile);
                    removed++;
                }

            return removed;
        }
    }
}
