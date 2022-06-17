// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
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

            Assert.That(host.ExecutionState, Is.EqualTo(ExecutionState.Running));

            // block game from exiting.
            game.BlockExit.Value = true;
            requestExit();
            Assert.That(game.LastExitResult, Is.True);

            // exit should be blocked.
            Assert.That(task.IsCompleted, Is.False);
            Assert.That(host.ExecutionState, Is.EqualTo(ExecutionState.Running));

            // unblock game from exiting.
            game.BlockExit.Value = false;
            requestExit();
            Assert.That(game.LastExitResult, Is.False);

            // finally, the game should exit.
            task.Wait(timeout);
            Assert.That(host.ExecutionState, Is.EqualTo(ExecutionState.Stopped));
        }

        private void requestExit()
        {
            host.RequestExit();

            // wait for the event to be handled by the game (on the update thread)
            Assert.That(game.ExitRequested.Wait(timeout), Is.True);
            game.ExitRequested.Reset();
        }

        private class ManualExitHeadlessGameHost : TestRunHeadlessGameHost
        {
            public void RequestExit() => OnExitRequested();
        }

        private class TestTestGame : TestGame
        {
            public readonly ManualResetEventSlim BecameAlive = new ManualResetEventSlim();
            public readonly ManualResetEventSlim ExitRequested = new ManualResetEventSlim();

            public bool? LastExitResult { get; private set; }

            protected override void LoadComplete()
            {
                BecameAlive.Set();
            }

            protected override bool OnExiting()
            {
                bool result = base.OnExiting();
                LastExitResult = result;
                ExitRequested.Set();
                return result;
            }
        }
    }
}
