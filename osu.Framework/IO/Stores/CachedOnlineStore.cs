// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using System.Threading.Tasks;

namespace osu.Framework.IO.Stores
{
    public class CachedOnlineStore : OnlineStore
    {
        public override Task<byte[]> GetAsync(string url) => throw new NotImplementedException();

        public override byte[] Get(string url) => throw new NotImplementedException();

        public override Stream GetStream(string url) => throw new NotImplementedException();
    }
}
