// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using System.Threading.Tasks;
using osu.Framework.Extensions;

namespace osu.Framework.IO.Stores
{
    public abstract class CachedOnlineStore : OnlineStore
    {
        protected abstract string CachePath { get; }

        protected virtual TimeSpan Duration => TimeSpan.FromDays(7);

        public override async Task<byte[]> GetAsync(string url)
        {
            var targetPath = getCached(url, out var data);

            if (data == null && (data = await base.GetAsync(url)) != null)
                cache(targetPath, data);

            return data;
        }

        public override byte[] Get(string url)
        {
            var targetPath = getCached(url, out var data);

            if (data == null && (data = base.Get(url)) != null)
                cache(targetPath, data);

            return data;
        }

        private void cache(string targetPath, byte[] data)
        {
            Directory.CreateDirectory(CachePath);
            System.IO.File.WriteAllBytes(targetPath, data);
        }

        private string getCached(string url, out byte[] data)
        {
            var targetPath = Path.Combine(CachePath, url.ComputeMD5Hash());

            if (System.IO.File.Exists(targetPath))
            {
                data = System.IO.File.ReadAllBytes(targetPath);
                System.IO.File.SetLastAccessTime(targetPath, DateTime.Now);
            }
            else
                data = null;

            return targetPath;
        }

        public int Clear()
        {
            return Clear(Duration);
        }

        public int Clear(TimeSpan duration)
        {
            var removed = 0;
            var cacheDirectory = new DirectoryInfo(CachePath);

            if (cacheDirectory.Exists)
            {
                var cachedFiles = cacheDirectory.GetFiles();

                foreach (var cachedFile in cachedFiles)
                    if (DateTime.Now - cachedFile.LastAccessTime > duration)
                    {
                        cachedFile.Delete();
                        removed++;
                    }
            }

            return removed;
        }
    }
}
