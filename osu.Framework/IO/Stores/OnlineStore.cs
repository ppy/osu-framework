// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.IO;
using System.Threading.Tasks;
using osu.Framework.IO.Network;

namespace osu.Framework.IO.Stores
{
    public class OnlineStore : IResourceStore<byte[]>
    {
        public async Task<byte[]> GetAsync(string url)
        {
            return await Task.Run(delegate
            {
                if (!url.StartsWith(@"https://"))
                    return null;

                try
                {
                    WebRequest req = new WebRequest($@"{url}");
                    req.BlockingPerform();
                    return req.ResponseData;
                }
                catch
                {
                    return null;
                }

            });
        }

        public byte[] Get(string url)
        {
            return GetAsync(url).Result;
        }

        public Stream GetStream(string url)
        {
            var ret = Get(url);

            if (ret == null) return null;

            return new MemoryStream(ret);
        }
    }
}
