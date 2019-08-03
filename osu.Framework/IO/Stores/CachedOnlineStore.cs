// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace osu.Framework.IO.Stores
{
    public class CachedOnlineStore : OnlineStore
    {
        public virtual string CachePath => Path.GetTempPath();

        public virtual TimeSpan Duration => TimeSpan.FromDays(7);

        public CachedOnlineStore()
        {
            Clear();
        }

        public override Task<byte[]> GetAsync(string url) => throw new NotImplementedException();

        public override byte[] Get(string url) => throw new NotImplementedException();

        public override Stream GetStream(string url) => throw new NotImplementedException();

        public void Clear()
        {
            Clear(Duration);
        }

        public void Clear(TimeSpan duration)
        {
            var cacheDirectory = new DirectoryInfo(CachePath);
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
