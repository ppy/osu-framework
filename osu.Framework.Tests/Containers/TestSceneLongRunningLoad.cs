// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Containers;
using osu.Framework.Testing;
using osu.Framework.Tests.Visual;
using osu.Framework.Threading;

namespace osu.Framework.Tests.Containers
{
    [HeadlessTest]
    public class TestSceneLongRunningLoad : FrameworkTestScene
    {
        [Test]
        public void TestSynchronousLoadLongRunningThrows()
        {
            AddStep("test incorrect usage", () => Assert.Throws<InvalidOperationException>(() => Add(new TestLoadBlockingDrawableLongRunning())));
            AddStep("test correct usage", () => LoadComponentAsync(new TestLoadBlockingDrawableLongRunning()));
        }

        [Test]
        public void TestSynchronousLoadDerivedLongRunningThrows()
        {
            AddStep("test incorrect usage", () => Assert.Throws<InvalidOperationException>(() => Add(new TestLoadBlockingDrawableLongRunningDerived())));
            AddStep("test correct usage", () => LoadComponentAsync(new TestLoadBlockingDrawableLongRunningDerived()));
        }

        [Test]
        public void TestLongRunningDoesntBlock()
        {
            List<TestLoadBlockingDrawableLongRunning> longRunning = new List<TestLoadBlockingDrawableLongRunning>();

            // add enough drawables to saturate the task scheduler
            AddRepeatStep("add long running", () =>
            {
                var d = new TestLoadBlockingDrawableLongRunning();
                longRunning.Add(d);
                LoadComponentAsync(d);
            }, 10);

            TestLoadBlockingDrawable normal = null;

            AddStep("add normal", () => { LoadComponentAsync(normal = new TestLoadBlockingDrawable(), Add); });

            AddStep("allow normal load", () => normal.AllowLoad.Set());

            AddUntilStep("did load", () => normal.IsLoaded);
        }

        [Test]
        public void TestLoadParentSynchronousThrows()
        {
            AddStep("test incorrect usage", () =>
            {
                Assert.Throws<InvalidOperationException>(() =>
                {
                    Add(new Container
                    {
                        Child = new TestLoadBlockingDrawableLongRunningDerived()
                    });
                });
            });
        }

        [Test]
        public void TestLoadParentAsynchronousThrows()
        {
            Scheduler scheduler = null;
            Exception exception = null;

            AddStep("begin long running", () =>
            {
                scheduler = new Scheduler();
                exception = null;

                LoadComponentAsync(new Container
                {
                    Child = new TestLoadBlockingDrawableLongRunningDerived()
                }, scheduler: scheduler);
            });

            AddUntilStep("wait for load to complete", () => scheduler.HasPendingTasks);

            AddStep("run scheduler", () =>
            {
                try
                {
                    scheduler.Update();
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
            });

            AddAssert("has thrown", () => exception is InvalidOperationException);
        }

        private class TestLoadBlockingDrawableLongRunningDerived : TestLoadBlockingDrawableLongRunning
        {
        }

        [LongRunningLoad]
        private class TestLoadBlockingDrawableLongRunning : CompositeDrawable
        {
            public readonly ManualResetEventSlim AllowLoad = new ManualResetEventSlim();

            [BackgroundDependencyLoader]
            private void load()
            {
                if (!AllowLoad.Wait(TimeSpan.FromSeconds(10)))
                    throw new TimeoutException();
            }
        }

        private class TestLoadBlockingDrawable : CompositeDrawable
        {
            public readonly ManualResetEventSlim AllowLoad = new ManualResetEventSlim();

            [BackgroundDependencyLoader]
            private void load()
            {
                if (!AllowLoad.Wait(TimeSpan.FromSeconds(10)))
                    throw new TimeoutException();
            }
        }
    }
}
