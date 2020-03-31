// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using osu.Framework.Platform;

namespace osu.Framework.Tests.Platform
{
    [TestFixture]
    public class GameHostSuspendTest
    {
        private TestTestGame game;
        private TestHeadlessGameHost host;

        private const int timeout = 10000;

        [Test]
        public void TestPauseResume()
        {
            var gameCreated = new ManualResetEventSlim();

            var task = Task.Run(() =>
            {
                using (host = new TestHeadlessGameHost(@"host", false))
                {
                    game = new TestTestGame();
                    gameCreated.Set();
                    host.Run(game);
                }
            });

            Assert.IsTrue(gameCreated.Wait(timeout));
            Assert.IsTrue(game.BecameAlive.Wait(timeout));

            // check scheduling is working before suspend
            var completed = new ManualResetEventSlim();
            game.Schedule(() => completed.Set());
            Assert.IsTrue(completed.Wait(timeout / 10));

            host.Suspend();

            completed.Reset();

            // check that scheduler doesn't process while suspended..
            game.Schedule(() => completed.Set());
            Assert.IsFalse(completed.Wait(timeout / 10));

            host.Resume();

            // ..and does after resume.
            Assert.IsTrue(completed.Wait(timeout / 10));

            game.Exit();

            Assert.IsTrue(task.Wait(timeout));
        }

        private class TestHeadlessGameHost : HeadlessGameHost
        {
            public TestHeadlessGameHost(string hostname, bool bindIPC)
                : base(hostname, bindIPC)
            {
            }
        }

        private class TestTestGame : TestGame
        {
            public readonly ManualResetEventSlim BecameAlive = new ManualResetEventSlim();

            protected override void LoadComplete()
            {
                BecameAlive.Set();
            }
        }
    }
}
