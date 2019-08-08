// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using System.Threading.Tasks;
using osu.Framework.Extensions;
using osu.Framework.Platform;

namespace osu.Framework.IO.Stores
{
    public class CachedOnlineStore : OnlineStore
    {
        private readonly Storage cacheStorage;
        public readonly TimeSpan Duration;

        public CachedOnlineStore(Storage cacheStorage, TimeSpan duration)
        {
            this.cacheStorage = cacheStorage;
            Duration = duration;
        }

        public override async Task<byte[]> GetAsync(string url)
        {
            var data = getCached(url);

            if (data == null && (data = await base.GetAsync(url)) != null)
                cache(url, data);

            return data;
        }

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

        private void cache(string url, byte[] data)
        {
            using (var stream = cacheStorage.GetStream(url.ComputeMD5Hash(), FileAccess.Write, FileMode.Create))
                stream.Write(data, 0, data.Length);
        }

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
