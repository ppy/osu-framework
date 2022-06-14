// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.IO;
using osu.Framework.Platform;

namespace osu.Framework.Testing
{
    /// <summary>
    /// A temporary storage that can be used for testing purposes.
    /// Writes files to the OS temporary directory and cleans up on disposal.
    /// </summary>
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
            try
            {
                DeleteDirectory(string.Empty);
            }
            catch
            {
            }
        }
    }
}
