// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Threading;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Extensions.TypeExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Logging;
using osu.Framework.Platform;

namespace osu.Framework.Testing
{
    public class TestSceneTestRunner : Game, ITestSceneTestRunner
    {
        private readonly TestRunner runner;

        public TestSceneTestRunner()
        {
            Add(runner = new TestRunner());
        }

        /// <summary>
        /// Blocks execution until a provided <see cref="TestScene"/> runs to completion.
        /// </summary>
        /// <param name="test">The <see cref="TestScene"/> to run.</param>
        public virtual void RunTestBlocking(TestScene test) => runner.RunTestBlocking(test);

        public class TestRunner : CompositeDrawable
        {
            private const double time_between_tests = 200;

            [Resolved]
            private GameHost host { get; set; }

            public TestRunner()
            {
                RelativeSizeAxes = Axes.Both;
            }

            /// <summary>
            /// Blocks execution until a provided <see cref="TestScene"/> runs to completion.
            /// </summary>
            /// <param name="test">The <see cref="TestScene"/> to run.</param>
            public void RunTestBlocking(TestScene test)
            {
                Trace.Assert(host != null, $"Ensure this runner has been loaded before calling {nameof(RunTestBlocking)}");

                bool completed = false;
                ExceptionDispatchInfo exception = null;

                void complete()
                {
                    // We want to remove the TestScene from the hierarchy on completion as under nUnit, it may have operations run on it from a different thread.
                    // This is because nUnit will reuse the same class multiple times, running a different [Test] method each time, while the GameHost
                    // is run from its own asynchronous thread.
                    RemoveInternal(test);
                    completed = true;
                }

                Schedule(() =>
                {
                    AddInternal(test);

                    Logger.Log($@"💨 Class: {test.GetType().ReadableName()}");
                    Logger.Log($@"🔶 Test:  {TestContext.CurrentContext.Test.Name}");

                    // Nunit will run the tests in the TestScene with the same TestScene instance so the TestScene
                    // needs to be removed before the host is exited, otherwise it will end up disposed

                    test.RunAllSteps(() =>
                    {
                        Scheduler.AddDelayed(complete, time_between_tests);
                    }, e =>
                    {
                        exception = ExceptionDispatchInfo.Capture(e);
                        complete();
                    });
                });

                while (!completed && host.ExecutionState == ExecutionState.Running)
                    Thread.Sleep(10);

                exception?.Throw();
            }
        }
    }

    public interface ITestSceneTestRunner
    {
        /// <summary>
        /// Blocks execution until a provided <see cref="TestScene"/> runs to completion.
        /// </summary>
        /// <param name="test">The <see cref="TestScene"/> to run.</param>
        void RunTestBlocking(TestScene test);
    }
}
