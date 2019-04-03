// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Platform;

namespace osu.Framework.Tests
{
    public class TemporaryNativeStorage : NativeStorage, IDisposable
    {
        public TemporaryNativeStorage(string name, GameHost host = null)
            : base(name, host)
        {
        }

        public void Dispose()
        {
            DeleteDirectory(string.Empty);
        }
    }
}
