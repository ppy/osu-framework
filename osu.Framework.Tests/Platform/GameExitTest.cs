// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using osu.Framework.Extensions;
using osu.Framework.Platform;
using osu.Framework.Testing;

namespace osu.Framework.Tests.Platform
{
    [TestFixture]
    public class GameExitTest
    {
        private TestTestGame game;
        private ManualExitHeadlessGameHost host;

        private const int timeout = 5000;

        [Test]
        public void TestExitBlocking()
        {
            var gameCreated = new ManualResetEventSlim();

            var task = Task.Factory.StartNew(() =>
            {
                using (host = new ManualExitHeadlessGameHost())
                {
                    game = new TestTestGame();
                    gameCreated.Set();
                    host.Run(game);
                }
            }, TaskCreationOptions.LongRunning);

            gameCreated.Wait(timeout);
            Assert.IsTrue(game.BecameAlive.Wait(timeout));

            // block game from exiting.
            game.BlockExit.Value = true;
            // `RequestExit()` should return true.
            Assert.That(host.RequestExit(), Is.True);
            // game's last exit result should match.
            Assert.That(game.LastExitResult, Is.True);
            // exit should be blocked.
            Assert.That(() => host.ExecutionState, Is.EqualTo(ExecutionState.Running).After(timeout));
            Assert.That(task.IsCompleted, Is.False);

            // unblock game from exiting.
            game.BlockExit.Value = false;
            // `RequestExit()` should not be blocked and return false.
            Assert.That(host.RequestExit(), Is.False);
            // game's last exit result should match.
            Assert.That(game.LastExitResult, Is.False);
            // finally, the game should exit.
            Assert.That(() => host.ExecutionState, Is.EqualTo(ExecutionState.Stopped).After(timeout));
            task.WaitSafely();
        }

        private class ManualExitHeadlessGameHost : TestRunHeadlessGameHost
        {
            public bool RequestExit() => OnExitRequested();
        }

        private class TestTestGame : TestGame
        {
            public readonly ManualResetEventSlim BecameAlive = new ManualResetEventSlim();

            public bool? LastExitResult { get; private set; }

            protected override void LoadComplete()
            {
                BecameAlive.Set();
            }

            protected override bool OnExiting()
            {
                bool result = base.OnExiting();
                LastExitResult = result;
                return result;
            }
        }
    }
}
