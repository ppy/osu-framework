// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Testing;
using osu.Framework.Tests.Visual;
using osu.Framework.Threading;

namespace osu.Framework.Tests.Containers
{
    [HeadlessTest]
    public class TestSceneLongRunningLoad : FrameworkTestScene
    {
        /// <summary>
        /// Tests that an exception is thrown when a long-running drawable is synchronously loaded.
        /// </summary>
        [Test]
        public void TestSynchronousLoadLongRunningThrows() => testSynchronousLoad(() => new TestLoadBlockingDrawableLongRunning(true), true);

        /// <summary>
        /// Tests that an exception is not thrown when a long-running drawable is asynchronously loaded.
        /// </summary>
        [Test]
        public void TestAsynchronousLoadLongRunningDoesNotThrow() => testAsynchronousLoad(() => new TestLoadBlockingDrawableLongRunning(true), false);

        /// <summary>
        /// Tests that an exception is thrown when a derived long-running drawable is synchronously loaded.
        /// </summary>
        [Test]
        public void TestSynchronousLoadDerivedLongRunningThrows() => testSynchronousLoad(() => new TestLoadBlockingDrawableLongRunningDerived(true), true);

        /// <summary>
        /// Tests that an exception is not thrown when a derived long-running drawable is asynchronously loaded.
        /// </summary>
        [Test]
        public void TestAsynchronousLoadDerivedLongRunningDoesNotThrow() => testAsynchronousLoad(() => new TestLoadBlockingDrawableLongRunningDerived(true), false);

        /// <summary>
        /// Tests that an exception is thrown when a parent is synchronously loaded and contains a long-running child.
        /// </summary>
        [Test]
        public void TestLoadParentSynchronousThrows() => testSynchronousLoad(() => new Container
        {
            Child = new TestLoadBlockingDrawableLongRunningDerived(true)
        }, true);

        /// <summary>
        /// Tests that an exception is thrown when a parent is asynchronously loaded and contains a long-running child.
        /// </summary>
        [Test]
        public void TestLoadParentAsynchronousThrows() => testAsynchronousLoad(() => new Container
        {
            Child = new TestLoadBlockingDrawableLongRunningDerived(true)
        }, true);

        /// <summary>
        /// Tests that long-running drawables don't block non-long running drawables from loading.
        /// </summary>
        [Test]
        public void TestLongRunningLoadDoesNotBlock()
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

            AddStep("allow long running load", () => longRunning.ForEach(d => d.AllowLoad.Set()));
        }

        private void testSynchronousLoad(Func<Drawable> context, bool shouldThrow)
        {
            AddAssert($"{(shouldThrow ? "has" : "has not")} thrown", () =>
            {
                try
                {
                    Add(context());
                }
                catch (InvalidOperationException)
                {
                    return shouldThrow;
                }

                return !shouldThrow;
            });
        }

        private void testAsynchronousLoad(Func<Drawable> context, bool shouldThrow)
        {
            Scheduler scheduler = null;

            AddStep("begin long running", () => LoadComponentAsync(context(), scheduler: scheduler = new Scheduler()));

            // Exceptions during async loads are thrown on the scheduler rather than on invocation
            AddUntilStep("wait for load to complete", () => scheduler.HasPendingTasks);

            AddAssert($"{(shouldThrow ? "has" : "has not")} thrown", () =>
            {
                try
                {
                    scheduler.Update();
                }
                catch (InvalidOperationException)
                {
                    return shouldThrow;
                }

                return !shouldThrow;
            });
        }

        private class TestLoadBlockingDrawableLongRunningDerived : TestLoadBlockingDrawableLongRunning
        {
            public TestLoadBlockingDrawableLongRunningDerived(bool allowLoad = false)
                : base(allowLoad)
            {
            }
        }

        [LongRunningLoad]
        private class TestLoadBlockingDrawableLongRunning : CompositeDrawable
        {
            public readonly ManualResetEventSlim AllowLoad = new ManualResetEventSlim();

            public TestLoadBlockingDrawableLongRunning(bool allowLoad = false)
            {
                if (allowLoad)
                    AllowLoad.Set();
            }

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

            public TestLoadBlockingDrawable(bool allowLoad = false)
            {
                if (allowLoad)
                    AllowLoad.Set();
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                if (!AllowLoad.Wait(TimeSpan.FromSeconds(10)))
                    throw new TimeoutException();
            }
        }
    }
}
