// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

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
