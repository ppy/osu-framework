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
            using (var gameCreated = new ManualResetEventSlim(false))
            {
                Task.Run(() =>
                {
                    try
                    {
                        testGame = new TestGame();
                        // ReSharper disable once AccessToDisposedClosure
                        gameCreated.Set();

                        Run(testGame);
                    }
                    catch
                    {
                        // may throw an unobserved exception if we don't handle here.
                    }
                });

                gameCreated.Wait();
                testGame.HasProcessed.Wait();
            }
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
