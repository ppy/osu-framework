// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.IO;
using osu.Framework.Logging;
using osu.Framework.Platform;

namespace osu.Framework.Testing
{
    /// <summary>
    /// A GameHost which writes to the system temporary directory, attempting to clean up after the test run completes.
    /// </summary>
    public class TestRunHeadlessGameHost : HeadlessGameHost
    {
        public override string UserStoragePath { get; }

        public TestRunHeadlessGameHost(string name = null, bool bindIPC = false, bool realtime = false, bool portableInstallation = false)
            : base(name, bindIPC, realtime, portableInstallation)
        {
            UserStoragePath = $"{Path.GetTempPath()}/of-test-headless";
        }

        protected override void Dispose(bool isDisposing)
        {
            // ensure no more log entries are written during cleanup.
            // there is a flush call in base.Dispose which seals the deal.
            Logger.Enabled = false;

            base.Dispose(isDisposing);

            Storage?.DeleteDirectory(string.Empty);
        }
    }
}
