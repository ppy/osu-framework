// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Configuration;
using osu.Framework.Development;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Platform;
using osu.Framework.Threading;
using osuTK;

namespace osu.Framework.Tests.Visual.Drawables
{
    public class TestSceneSynchronizationContext : FrameworkTestScene
    {
        [Resolved]
        private GameHost host { get; set; }

        [Resolved]
        private FrameworkConfigManager config { get; set; }

        private AsyncPerformingBox box;

        [Test]
        public void TestAsyncLoadComplete()
        {
            AddStep("add box", () => Child = box = new AsyncPerformingBox(true));
            AddAssert("not spun", () => box.Rotation == 0);
            AddStep("trigger", () => box.ReleaseAsyncLoadCompleteLock());
            AddUntilStep("has spun", () => box.Rotation == 180);
        }

        private GameThreadSynchronizationContext syncContext => SynchronizationContext.Current as GameThreadSynchronizationContext;

        [Test]
        public void TestNoAsyncDoesntUseScheduler()
        {
            int initialTasksRun = 0;
            AddStep("get initial run count", () => initialTasksRun = syncContext.TotalTasksRun);
            AddStep("add box", () => Child = box = new AsyncPerformingBox(false));
            AddAssert("no tasks run", () => syncContext.TotalTasksRun == initialTasksRun);
            AddStep("trigger", () => box.ReleaseAsyncLoadCompleteLock());
            AddAssert("no tasks run", () => syncContext.TotalTasksRun == initialTasksRun);
        }

        [Test]
        public void TestAsyncUsesScheduler()
        {
            int initialTasksRun = 0;
            AddStep("get initial run count", () => initialTasksRun = syncContext.TotalTasksRun);
            AddStep("add box", () => Child = box = new AsyncPerformingBox(true));
            AddAssert("no tasks run", () => syncContext.TotalTasksRun == initialTasksRun);
            AddStep("trigger", () => box.ReleaseAsyncLoadCompleteLock());
            AddUntilStep("one new task run", () => syncContext.TotalTasksRun == initialTasksRun + 1);
        }

        [Test]
        public void TestOrderOfExecutionFlushing()
        {
            List<int> ran = new List<int>();

            AddStep("queue items", () =>
            {
                SynchronizationContext.Current?.Post(_ => ran.Add(1), null);
                SynchronizationContext.Current?.Post(_ => ran.Add(2), null);
                SynchronizationContext.Current?.Post(_ => ran.Add(3), null);

                Assert.That(ran, Is.Empty);

                SynchronizationContext.Current?.Send(_ => ran.Add(4), null);

                Assert.That(ran, Is.EqualTo(new[] { 1, 2, 3, 4 }));
            });
        }

        [Test]
        public void TestOrderOfExecutionFlushingAsyncThread()
        {
            ManualResetEventSlim finished = new ManualResetEventSlim();
            List<int> ran = new List<int>();

            AddStep("queue items", () =>
            {
                var updateContext = SynchronizationContext.Current;

                Debug.Assert(updateContext != null);

                updateContext.Post(_ => ran.Add(1), null);
                updateContext.Post(_ => ran.Add(2), null);
                updateContext.Post(_ => ran.Add(3), null);

                Assert.That(ran, Is.Empty);

                Task.Factory.StartNew(() =>
                {
                    updateContext.Send(_ => ran.Add(4), null);

                    Assert.That(ran, Is.EqualTo(new[] { 1, 2, 3, 4 }));

                    finished.Set();
                }, TaskCreationOptions.LongRunning);
            });

            AddUntilStep("wait for completion", () => finished.IsSet);
        }

        [Test]
        public void TestAsyncThrows()
        {
            Exception thrown = null;

            AddStep("watch for exceptions", () => host.ExceptionThrown += onException);
            AddStep("throw on update thread", () =>
            {
                // ReSharper disable once AsyncVoidLambda
                host.UpdateThread.Scheduler.Add(async () =>
                {
                    Assert.That(ThreadSafety.IsUpdateThread);

                    await Task.Delay(100).ConfigureAwait(true);

                    Assert.That(ThreadSafety.IsUpdateThread);

                    throw new InvalidOperationException();
                });
            });

            AddUntilStep("wait for exception to arrive", () => thrown is InvalidOperationException);
            AddStep("stop watching for exceptions", () => host.ExceptionThrown -= onException);

            bool onException(Exception arg)
            {
                thrown = arg;
                return true;
            }
        }

        [Test]
        public void TestAsyncInsideUpdate()
        {
            int updateCount = 0;

            AddStep("add box", () =>
            {
                updateCount = 0;
                Child = box = new AsyncPerformingBox(false);
            });

            AddUntilStep("has spun", () => box.Rotation == 180);

            AddStep("update with async", () =>
            {
#pragma warning disable 4014
                box.OnUpdate += _ => asyncAction();
#pragma warning restore 4014

                async Task asyncAction()
                {
                    updateCount++;
                    await box.PerformAsyncWait().ConfigureAwait(true);
                    box.RotateTo(0, 500);
                }
            });

            AddUntilStep("is running updates", () => updateCount > 5);
            AddStep("trigger", () => box.ReleaseAsyncLoadCompleteLock());
            AddUntilStep("has spun", () => box.Rotation == 0);
        }

        [Test]
        public void TestAsyncInsideSchedule()
        {
            AddStep("add box", () => Child = box = new AsyncPerformingBox(false));
            AddUntilStep("has spun", () => box.Rotation == 180);

            AddStep("schedule with async", () =>
            {
#pragma warning disable 4014
                // We may want to give `Schedule` a `Task` accepting overload in the future.
                box.Schedule(() => asyncScheduledAction());
#pragma warning restore 4014

                async Task asyncScheduledAction()
                {
                    await box.PerformAsyncWait().ConfigureAwait(true);
                    box.RotateTo(0, 500);
                }
            });

            AddStep("trigger", () => box.ReleaseAsyncLoadCompleteLock());
            AddUntilStep("has spun", () => box.Rotation == 0);
        }

        [Test]
        public void TestExecutionMode()
        {
            AddStep("add box", () => Child = box = new AsyncPerformingBox(true));
            AddAssert("not spun", () => box.Rotation == 0);

            AddStep("toggle execution mode", () => toggleExecutionMode());

            AddStep("trigger", () => box.ReleaseAsyncLoadCompleteLock());
            AddUntilStep("has spun", () => box.Rotation == 180);

            AddStep("revert execution mode", () => toggleExecutionMode());

            void toggleExecutionMode()
            {
                var executionMode = config.GetBindable<ExecutionMode>(FrameworkSetting.ExecutionMode);

                executionMode.Value = executionMode.Value == ExecutionMode.MultiThreaded
                    ? ExecutionMode.SingleThread
                    : ExecutionMode.MultiThreaded;
            }
        }

        public class AsyncPerformingBox : Box
        {
            private readonly bool performAsyncLoadComplete;

            private readonly SemaphoreSlim waiter = new SemaphoreSlim(0);

            public AsyncPerformingBox(bool performAsyncLoadComplete)
            {
                this.performAsyncLoadComplete = performAsyncLoadComplete;

                Size = new Vector2(100);

                Anchor = Anchor.Centre;
                Origin = Anchor.Centre;
            }

            protected override async void LoadComplete()
            {
                base.LoadComplete();

                if (performAsyncLoadComplete)
                    await PerformAsyncWait().ConfigureAwait(true);

                this.RotateTo(180, 500);
            }

            public async Task PerformAsyncWait() => await waiter.WaitAsync().ConfigureAwait(false);

            public void ReleaseAsyncLoadCompleteLock() => waiter.Release();
        }
    }
}
