// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Shapes;
using osuTK;

namespace osu.Framework.Tests.Visual.Drawables
{
    public class TestSceneSynchronizationContext : FrameworkTestScene
    {
        private AsyncPerformingBox box;

        [Test]
        public void TestAsyncLoadComplete()
        {
            AddStep("add box", () => Child = box = new AsyncPerformingBox(true));
            AddAssert("not spun", () => box.Rotation == 0);
            AddStep("trigger", () => box.ReleaseAsyncLoadCompleteLock());
            AddUntilStep("has spun", () => box.Rotation == 180);
        }

        [Test]
        public void TestNoAsyncDoesntUseScheduler()
        {
            AddStep("add box", () => Child = box = new AsyncPerformingBox(false));
            AddAssert("scheduler run count 0", () => box.Scheduler.TotalTasksRun == 0);
            AddStep("trigger", () => box.ReleaseAsyncLoadCompleteLock());
            AddAssert("scheduler run count 0", () => box.Scheduler.TotalTasksRun == 0);
        }

        [Test]
        public void TestAsyncUsesScheduler()
        {
            AddStep("add box", () => Child = box = new AsyncPerformingBox(true));
            AddAssert("scheduler run count 0", () => box.Scheduler.TotalTasksRun == 0);
            AddStep("trigger", () => box.ReleaseAsyncLoadCompleteLock());
            AddAssert("scheduler run count 1", () => box.Scheduler.TotalTasksRun == 1);
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
