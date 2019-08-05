// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

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
            var targetPath = Path.Combine(CachePath, createHash64(url).ToString());

            if (System.IO.File.Exists(targetPath))
            {
                data = System.IO.File.ReadAllBytes(targetPath);
                System.IO.File.SetLastAccessTime(targetPath, DateTime.Now);
            }
            else
                data = null;

            return targetPath;
        }

        public void Clear()
        {
            Clear(Duration);
        }

        public void Clear(TimeSpan duration)
        {
            var cacheDirectory = new DirectoryInfo(CachePath);

            if (!cacheDirectory.Exists)
                return;

            var cachedFiles = cacheDirectory.GetFiles();

            foreach (var cachedFile in cachedFiles)
                if (DateTime.Now - cachedFile.LastAccessTime > duration)
                    cachedFile.Delete();
        }

        private ulong createHash64(string str)
        {
            var utf8 = Encoding.UTF8.GetBytes(str);
            var value = (ulong)utf8.Length;

            for (int n = 0; n < utf8.Length; n++)
                value += (ulong)utf8[n] << (n * 5 % 56);

            return value;
        }
    }
}
