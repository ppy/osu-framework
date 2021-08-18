// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using osu.Framework.Platform;
using osu.Framework.Testing;

namespace osu.Framework.Tests
{
    public class TemporaryNativeStorage : NativeStorage, IDisposable
    {
        public TemporaryNativeStorage(string path, GameHost host = null)
            : base(Path.Combine(TestRunHeadlessGameHost.TemporaryTestDirectory, path), host)
        {
            // create directory
            GetFullPath("./", true);
        }

        public void Dispose()
        {
            DeleteDirectory(string.Empty);
        }
    }
}
