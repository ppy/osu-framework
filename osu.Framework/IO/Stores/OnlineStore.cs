// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WebRequest = osu.Framework.IO.Network.WebRequest;

namespace osu.Framework.IO.Stores
{
    public class OnlineStore : IResourceStore<byte[]>
    {
        public async Task<byte[]> GetAsync(string url, CancellationToken cancellationToken = default)
        {
            if (!validateScheme(url))
                return null;

            this.LogIfNonBackgroundThread(url);

            try
            {
                using (WebRequest req = new WebRequest(GetLookupUrl(url)))
                {
                    await req.PerformAsync(cancellationToken).ConfigureAwait(false);
                    return req.GetResponseData();
                }
            }
            catch
            {
                return null;
            }
        }

        public virtual byte[] Get(string url)
        {
            if (!validateScheme(url))
                return null;

            this.LogIfNonBackgroundThread(url);

            try
            {
                using (WebRequest req = new WebRequest(GetLookupUrl(url)))
                {
                    req.Perform();
                    return req.GetResponseData();
                }
            }
            catch
            {
                return null;
            }
        }

        public Stream GetStream(string url)
        {
            byte[] ret = Get(url);

            if (ret == null) return null;

            return new MemoryStream(ret);
        }

        public IEnumerable<string> GetAvailableResources() => Enumerable.Empty<string>();

        /// <summary>
        /// Returns the URL used to look up the requested resource.
        /// </summary>
        /// <param name="url">The original URL for lookup.</param>
        protected virtual string GetLookupUrl(string url) => url;

        private bool validateScheme(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uri))
                return false;

            return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
        }

        #region IDisposable Support

        public void Dispose()
        {
        }

        #endregion
    }
}
