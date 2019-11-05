// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

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
        public void TestSynchronousLoadLongRunningThrows() => testSynchronousLoad(() => new TestLoadBlockingDrawableLongRunning(), true);

        /// <summary>
        /// Tests that an exception is not thrown when a long-running drawable is asynchronously loaded.
        /// </summary>
        [Test]
        public void TestAsynchronousLoadLongRunningDoesNotThrow() => testAsynchronousLoad(() => new TestLoadBlockingDrawableLongRunning(), false);

        /// <summary>
        /// Tests that an exception is thrown when a derived long-running drawable is synchronously loaded.
        /// </summary>
        [Test]
        public void TestSynchronousLoadDerivedLongRunningThrows() => testSynchronousLoad(() => new TestLoadBlockingDrawableLongRunningDerived(), true);

        /// <summary>
        /// Tests that an exception is not thrown when a derived long-running drawable is asynchronously loaded.
        /// </summary>
        [Test]
        public void TestAsynchronousLoadDerivedLongRunningDoesNotThrow() => testAsynchronousLoad(() => new TestLoadBlockingDrawableLongRunningDerived(), false);

        /// <summary>
        /// Tests that long-running drawables finish loading.
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
        }

        /// <summary>
        /// Tests that an exception is thrown when a parent is synchronously loaded and contains a long-running child.
        /// </summary>
        [Test]
        public void TestLoadParentSynchronousThrows() => testSynchronousLoad(() => new Container
        {
            Child = new TestLoadBlockingDrawableLongRunningDerived()
        }, true);

        /// <summary>
        /// Tests that an exception is thrown when a parent is asynchronously loaded and contains a long-running child.
        /// </summary>
        [Test]
        public void TestLoadParentAsynchronousThrows() => testAsynchronousLoad(() => new Container
        {
            Child = new TestLoadBlockingDrawableLongRunningDerived()
        }, true);

        private void testSynchronousLoad(Func<Drawable> context, bool shouldThrow)
        {
            AddAssert($"{(shouldThrow ? "has" : "has not")} thrown", () =>
            {
                try
                {
                    Add(new TestLoadBlockingDrawableLongRunning();
                }
                catch (InvalidOperationException ex)
                {
                    return true;
                }

                return false;
            });
        }

        private void testAsynchronousLoad(Func<Drawable> context, bool shouldThrow)
        {
            Scheduler scheduler = null;
            Exception exception = null;

            AddStep("begin long running", () =>
            {
                scheduler = new Scheduler();
                exception = null;

                LoadComponentAsync(context(), scheduler: scheduler);
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

            AddAssert($"{(shouldThrow ? "has" : "has not")} thrown", () => (exception is InvalidOperationException) == shouldThrow);
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
