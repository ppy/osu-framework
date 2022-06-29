// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Logging;
using osu.Framework.Platform;

namespace osu.Framework.Testing
{
    /// <summary>
    /// A GameHost which writes to the system temporary directory, attempting to clean up after the test run completes.
    /// Also runs in non-realtime (allowing faster test execution) by default.
    /// </summary>
    public class TestRunHeadlessGameHost : HeadlessGameHost
    {
        private readonly bool bypassCleanup;

        public override IEnumerable<string> UserStoragePaths { get; }

        public static string TemporaryTestDirectory = Path.Combine(Path.GetTempPath(), "of-test-headless");

        [Obsolete("Use TestRunHeadlessGameHost(string, HostOptions, bool, bool) instead.")] // Can be removed 20220715
        public TestRunHeadlessGameHost(string gameName, bool bindIPC = false, bool realtime = false, bool portableInstallation = false, bool bypassCleanup = false)
            : this(gameName, new HostOptions
            {
                BindIPC = bindIPC,
                PortableInstallation = portableInstallation,
            }, bypassCleanup, realtime)
        {
        }

        public TestRunHeadlessGameHost(string gameName = null, HostOptions options = null, bool bypassCleanup = false, bool realtime = false)
            : base(gameName, options, realtime)
        {
            this.bypassCleanup = bypassCleanup;
            UserStoragePaths = TemporaryTestDirectory.Yield();
        }

        protected override void Dispose(bool isDisposing)
        {
            // ensure no more log entries are written during cleanup.
            // there is a flush call in base.Dispose which seals the deal.
            Logger.Enabled = false;

            base.Dispose(isDisposing);

            if (!bypassCleanup)
            {
                try
                {
                    Storage.DeleteDirectory(string.Empty);
                }
                catch
                {
                }
            }
        }
    }
}
