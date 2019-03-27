// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Threading;
using System.Threading.Tasks;
using osu.Framework.Platform;

namespace osu.Framework.Tests.IO
{
    /// <summary>
    /// Ad headless host for testing purposes. Contains an arbitrary game that is running after construction.
    /// </summary>
    public class BackgroundGameHeadlessGameHost : HeadlessGameHost
    {
        private TestGame testGame;

        public BackgroundGameHeadlessGameHost(string gameName = @"", bool bindIPC = false, bool realtime = true, bool portableInstallation = false)
            : base(gameName, bindIPC, realtime, portableInstallation)
        {
            Task.Run(() =>
            {
                try
                {
                    Run(testGame = new TestGame());
                }
                catch
                {
                    // may throw an unobserved exception if we don't handle here.
                }
            });

            while (testGame?.HasProcessed != true)
                Thread.Sleep(10);
        }

        private class TestGame : Game
        {
            public bool HasProcessed;

            protected override void Update()
            {
                base.Update();
                HasProcessed = true;
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
