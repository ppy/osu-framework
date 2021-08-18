// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.IO;
using osu.Framework.Logging;

namespace osu.Framework.Platform
{
    /// <summary>
    /// A GameHost which writes to the system temporary directory, attempting to clean up after the test run completes.
    /// </summary>
    public class TestHeadlessGameHost : HeadlessGameHost
    {
        public override string UserStoragePath { get; }

        public TestHeadlessGameHost(string name = null, bool bindIPC = false, bool realtime = false, bool portableInstallation = false)
            : base(name, bindIPC, realtime, portableInstallation)
        {
            UserStoragePath = $"{Path.GetTempPath()}/of-test-headless";
        }

        protected override void Dispose(bool isDisposing)
        {
            Logger.Enabled = false;
            base.Dispose(isDisposing);
            Storage?.DeleteDirectory(string.Empty);
        }
    }
}
