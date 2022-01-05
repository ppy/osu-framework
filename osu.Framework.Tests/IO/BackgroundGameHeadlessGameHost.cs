// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Threading;
using System.Threading.Tasks;
using osu.Framework.Platform;
using osu.Framework.Testing;

namespace osu.Framework.Tests.IO
{
    /// <summary>
    /// A headless host for testing purposes. Contains an arbitrary game that is running after construction.
    /// </summary>
    public class BackgroundGameHeadlessGameHost : TestRunHeadlessGameHost
    {
        [Obsolete("Use BackgroundGameHeadlessGameHost(HostConfig) instead.")]
        public BackgroundGameHeadlessGameHost(string gameName, bool bindIPC = false, bool realtime = true, bool portableInstallation = false)
            : this(new HostOptions
            {
                Name = gameName,
                BindIPC = bindIPC,
                PortableInstallation = portableInstallation,
            }, realtime)
        {
        }

        public BackgroundGameHeadlessGameHost(HostOptions options = null, bool realtime = true)
            : base(options, realtime: realtime)
        {
            var testGame = new TestGame();

            Task.Factory.StartNew(() => Run(testGame), TaskCreationOptions.LongRunning);

            testGame.HasProcessed.Wait();
        }

        private class TestGame : Game
        {
            internal readonly ManualResetEventSlim HasProcessed = new ManualResetEventSlim(false);

            protected override void Update()
            {
                base.Update();
                HasProcessed.Set();
            }

            protected override void Dispose(bool isDisposing)
            {
                HasProcessed.Dispose();
                base.Dispose(isDisposing);
            }
        }

        protected override void Dispose(bool isDisposing)
        {
            if (ExecutionState != ExecutionState.Stopped)
                Exit();
            base.Dispose(isDisposing);
        }
    }
}
