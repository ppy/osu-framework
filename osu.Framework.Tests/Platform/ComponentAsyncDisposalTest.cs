// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Containers;
using osu.Framework.Platform;
using osu.Framework.Testing;

namespace osu.Framework.Tests.Platform
{
    [TestFixture]
    public class ComponentAsyncDisposalTest
    {
        private TestTestGame game;
        private HeadlessGameHost host;

        private const int timeout = 10000;

        /// <summary>
        /// Ensure that if a component is on the async disposal queue, the game will wait for the queue to empty before disposing itself.
        /// While we generally don't dispose games in normal execution, this may be used in tests, and can cause unwanted exceptions in (cancelled) load
        /// methods if not ordered correctly.
        /// </summary>
        [Test]
        public void TestChildDisposedBeforeGame()
        {
            var gameCreated = new ManualResetEventSlim();

            var task = Task.Factory.StartNew(() =>
            {
                using (host = new TestRunHeadlessGameHost("host", new HostOptions(), bypassCleanup: false))
                {
                    game = new TestTestGame();
                    gameCreated.Set();
                    host.Run(game);
                }
            }, TaskCreationOptions.LongRunning);

            Assert.IsTrue(gameCreated.Wait(timeout));
            Assert.IsTrue(game.BecameAlive.Wait(timeout));

            var container = new DisposableContainer();

            game.Schedule(() => game.Add(container));

            Assert.IsTrue(container.BecameAlive.Wait(timeout));

            game.Schedule(() =>
            {
                game.ClearInternal(false);
                game.DisposeChildAsync(container);
            });

            game.Exit();

            Assert.IsTrue(task.Wait(timeout));

            game.Dispose();

            Assert.IsTrue(container.DisposedSuccessfully.Wait(timeout));
        }

        private class DisposableContainer : Container
        {
            [Resolved]
            private TestTestGame game { get; set; }

            public readonly ManualResetEventSlim BecameAlive = new ManualResetEventSlim();

            public readonly ManualResetEventSlim DisposedSuccessfully = new ManualResetEventSlim();

            protected override void LoadComplete()
            {
                BecameAlive.Set();
            }

            protected override void Dispose(bool isDisposing)
            {
                game.DisposalStarted.Wait(timeout);

                // make sure we take some time to dispose (forcing Game to wait on the async queue).
                Thread.Sleep(100);

                // we want to ensure that game is still alive pending our disposal.
                if (!game.IsDisposed)
                    DisposedSuccessfully.Set();

                base.Dispose(isDisposing);
            }
        }

        [Cached]
        private class TestTestGame : TestGame
        {
            public readonly ManualResetEventSlim BecameAlive = new ManualResetEventSlim();

            public readonly ManualResetEventSlim DisposalStarted = new ManualResetEventSlim();

            public readonly ManualResetEventSlim DisposalCompleted = new ManualResetEventSlim();

            protected override void LoadComplete()
            {
                BecameAlive.Set();
            }

            protected override void Dispose(bool isDisposing)
            {
                DisposalStarted.Set();
                base.Dispose(isDisposing);
                DisposalCompleted.Set();
            }
        }
    }
}
