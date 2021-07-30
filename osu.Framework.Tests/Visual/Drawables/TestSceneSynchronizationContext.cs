// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Threading;
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
        public void TestAsyncPerformingBox()
        {
            AddStep("add box", () => Child = box = new AsyncPerformingBox());
            AddAssert("not spun", () => box.Rotation == 0);
            AddStep("trigger", () => box.Trigger());
            AddUntilStep("has spun", () => box.Rotation == 180);
        }

        [Test]
        public void TestUsingLocalScheduler()
        {
            AddStep("add box", () => Child = box = new AsyncPerformingBox());
            AddAssert("scheduler null", () => box.Scheduler == null);
            AddStep("trigger", () => box.Trigger());
            AddUntilStep("scheduler non-null", () => box.Scheduler != null);
        }

        public class AsyncPerformingBox : Box
        {
            private readonly SemaphoreSlim waiter = new SemaphoreSlim(0);

            public AsyncPerformingBox()
            {
                Size = new Vector2(100);

                Anchor = Anchor.Centre;
                Origin = Anchor.Centre;
            }

            protected override async void LoadComplete()
            {
                base.LoadComplete();

                await waiter.WaitAsync().ConfigureAwait(true);

                this.RotateTo(180, 500);
            }

            public void Trigger()
            {
                waiter.Release();
            }
        }
    }
}
