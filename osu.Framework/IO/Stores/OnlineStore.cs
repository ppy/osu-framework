// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WebRequest = osu.Framework.IO.Network.WebRequest;

namespace osu.Framework.IO.Stores
{
    public class OnlineStore : IResourceStore<byte[]>
    {
        public async Task<byte[]> GetAsync(string url)
        {
            this.LogIfNonBackgroundThread(url);

            try
            {
                using (WebRequest req = new WebRequest($@"{url}"))
                {
                    await req.PerformAsync();
                    return req.ResponseData;
                }
            }
            catch
            {
                return null;
            }
        }

        public byte[] Get(string url)
        {
            if (!url.StartsWith(@"https://", StringComparison.Ordinal))
                return null;

            this.LogIfNonBackgroundThread(url);

            try
            {
                using (WebRequest req = new WebRequest($@"{url}"))
                {
                    req.Perform();
                    return req.ResponseData;
                }
            }
            catch
            {
                return null;
            }
        }

        public Stream GetStream(string url)
        {
            var ret = Get(url);

            if (ret == null) return null;

            return new MemoryStream(ret);
        }

        public IEnumerable<string> GetAvailableResources() => Enumerable.Empty<string>();

        #region IDisposable Support

        private bool isDisposed;

        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                isDisposed = true;
            }
        }

        ~OnlineStore()
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
