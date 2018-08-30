// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Platform;
using OpenTK;
using OpenTK.Graphics;

namespace osu.Framework.Tests.Exceptions
{
    [TestFixture]
    public class TestLoadExceptions
    {
        [Test]
        public void TestLoadIntoInvalidTarget()
        {
            var loadable = new DelayedTestBoxAsync();
            var loadTarget = new LoadTarget(loadable);

            Assert.Throws<InvalidOperationException>(() => loadTarget.PerformAsyncLoad());
        }

        [Test]
        public void TestSingleSyncAdd()
        {
            var loadable = new DelayedTestBoxAsync();

            Assert.DoesNotThrow(() =>
            {
                runGameWithLogic(g =>
                {
                    g.Add(loadable);
                    Assert.IsTrue(loadable.LoadState == LoadState.Ready);
                    Assert.AreEqual(loadable.Parent, g);
                    g.Exit();
                });
            });
        }

        [Test]
        public void TestSingleAsyncAdd()
        {
            var loadable = new DelayedTestBoxAsync();
            var loadTarget = new LoadTarget(loadable);

            Assert.DoesNotThrow(() =>
            {
                runGameWithLogic(g =>
                {
                    g.Add(loadTarget);
                    loadTarget.PerformAsyncLoad();
                }, g => loadable.Parent == loadTarget);
            });
        }

        [Test]
        public void TestDoubleAsyncLoad()
        {
            var loadable = new DelayedTestBoxAsync();
            var loadTarget = new LoadTarget(loadable);

            Assert.DoesNotThrow(() =>
            {
                runGameWithLogic(g =>
                {
                    g.Add(loadTarget);
                    loadTarget.PerformAsyncLoad();
                    loadTarget.PerformAsyncLoad(false);
                }, g => loadable.Parent == loadTarget);
            });
        }

        [Test]
        public void TestDoubleAsyncAddFails()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                runGameWithLogic(g =>
                {
                    var loadable = new DelayedTestBoxAsync();
                    var loadTarget = new LoadTarget(loadable);

                    g.Add(loadTarget);

                    loadTarget.PerformAsyncLoad();
                    loadTarget.PerformAsyncLoad();
                });
            });
        }

        [Test]
        public void TestTargetDisposedDuringAsyncLoad()
        {
            Assert.Throws<ObjectDisposedException>(() =>
            {
                runGameWithLogic(g =>
                {
                    var loadable = new DelayedTestBoxAsync();
                    var loadTarget = new LoadTarget(loadable);

                    g.Add(loadTarget);

                    loadTarget.PerformAsyncLoad();

                    while (loadable.LoadState < LoadState.Loading)
                        Thread.Sleep(1);

                    g.Dispose();
                });
            });
        }

        [Test]
        public void TestLoadableDisposedDuringAsyncLoad()
        {
            Assert.Throws<ObjectDisposedException>(() =>
            {
                runGameWithLogic(g =>
                {
                    var loadable = new DelayedTestBoxAsync();
                    var loadTarget = new LoadTarget(loadable);

                    g.Add(loadTarget);

                    loadTarget.PerformAsyncLoad();

                    while (loadable.LoadState < LoadState.Loading)
                        Thread.Sleep(1);

                    loadable.Dispose();
                });
            });
        }

        /// <summary>
        /// The async load completion callback is scheduled on the <see cref="Game"/>. The callback is generally used to add the child to the container,
        /// however it is possible for the container to be disposed when this occurs due to being scheduled on the <see cref="Game"/>. If this occurs,
        /// the cancellation is invoked and the completion task should not be run.
        ///
        /// This is a very timing-dependent test which performs the following sequence:
        /// LoadAsync -> schedule Callback -> dispose parent -> invoke scheduled callback
        /// </summary>
        [Test]
        public void TestDisposeAfterLoad()
        {
            Assert.DoesNotThrow(() =>
            {
                var loadTarget = new LoadTarget(new DelayedTestBoxAsync());

                bool allowDispose = false;
                bool disposeTriggered = false;
                bool updatedAfterDispose = false;

                runGameWithLogic(g =>
                {
                    g.Add(loadTarget);
                    loadTarget.PerformAsyncLoad().ContinueWith(t => allowDispose = true);
                }, g =>
                {
                    // The following code is done here for a very specific reason, but can occur naturally in normal use
                    // This delegate is essentially the first item in the game's scheduler, so it will always run PRIOR to the async callback

                    if (disposeTriggered)
                        updatedAfterDispose = true;

                    if (allowDispose)
                    {
                        // Async load has complete, the callback has been scheduled but NOT run yet
                        // Dispose the parent container - this is done by clearing the game
                        g.Clear(true);
                        disposeTriggered = true;
                    }

                    // After disposing the parent, one update loop is required
                    return updatedAfterDispose;
                });
            });
        }

        [Test]
        public void TestSyncLoadException()
        {
            Assert.Throws<AsyncTestException>(() => runGameWithLogic(g => g.Add(new DelayedTestBoxAsync(true))));
        }

        [SuppressMessage("ReSharper", "AccessToDisposedClosure")]
        private void runGameWithLogic(Action<Game> logic, Func<Game, bool> exitCondition = null)
        {
            using (var host = new HeadlessGameHost($"test-{Guid.NewGuid()}", realtime: false))
            using (var game = new TestGame())
            {
                game.Schedule(() => logic(game));
                host.UpdateThread.Scheduler.AddDelayed(() =>
                {
                    if (exitCondition?.Invoke(game) == true)
                        host.Exit();
                }, 0, true);

                host.Run(game);
            }
        }

        private class LoadTarget : Container
        {
            private readonly Drawable loadable;

            public LoadTarget(Drawable loadable)
            {
                this.loadable = loadable;
            }

            public Task PerformAsyncLoad(bool withAdd = true) => LoadComponentAsync(loadable, _ =>
            {
                if (withAdd) Add(loadable);
            });
        }

        public class DelayedTestBoxAsync : Box
        {
            private readonly bool throws;

            public DelayedTestBoxAsync(bool throws = false)
            {
                this.throws = throws;
                Size = new Vector2(50);
                Colour = Color4.Green;
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                Task.Delay((int)(1000 / Clock.Rate)).Wait();
                if (throws)
                    throw new AsyncTestException();
            }
        }

        private class AsyncTestException : Exception
        {
        }
    }
}
