﻿// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.IO;
using System.Threading.Tasks;
using WebRequest = osu.Framework.IO.Network.WebRequest;

namespace osu.Framework.IO.Stores
{
    public class OnlineStore : IResourceStore<byte[]>
    {
        public async Task<byte[]> GetAsync(string url)
        {
            if (!url.StartsWith(@"https://", StringComparison.Ordinal))
                return null;

            try
            {
                WebRequest req = new WebRequest($@"{url}");
                await req.PerformAsync();
                return req.ResponseData;
            }
            catch
            {
                return null;
            }
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
