// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Diagnostics;
using System.Threading;
using osu.Framework.Allocation;
using osu.Framework.Configuration;
using osu.Framework.Platform;
using osu.Framework.Screens;

namespace osu.Framework.Testing
{
    public class TestCaseTestRunner : Game, ITestCaseTestRunner
    {
        private readonly TestRunner runner;

        public TestCaseTestRunner()
        {
            Add(runner = new TestRunner());
        }

        /// <summary>
        /// Blocks execution until a provided <see cref="TestCase"/> runs to completion.
        /// </summary>
        /// <param name="test">The <see cref="TestCase"/> to run.</param>
        public void RunTestBlocking(TestCase test) => runner.RunTestBlocking(test);

        public class TestRunner : Screen
        {
            private const double time_between_tests = 200;

            private Bindable<double> volume;
            private double volumeAtStartup;

            private GameHost host;

            [BackgroundDependencyLoader]
            private void load(GameHost host, FrameworkConfigManager config)
            {
                this.host = host;

                volume = config.GetBindable<double>(FrameworkSetting.VolumeUniversal);
                volumeAtStartup = volume.Value;
                volume.Value = 0;
            }

            protected override void Dispose(bool isDisposing)
            {
                if (volume != null) volume.Value = volumeAtStartup;
                base.Dispose(isDisposing);
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();

                host.MaximumDrawHz = int.MaxValue;
                host.MaximumUpdateHz = int.MaxValue;
                host.MaximumInactiveHz = int.MaxValue;
            }

            /// <summary>
            /// Blocks execution until a provided <see cref="TestCase"/> runs to completion.
            /// </summary>
            /// <param name="test">The <see cref="TestCase"/> to run.</param>
            public void RunTestBlocking(TestCase test)
            {
                Trace.Assert(host != null, $"Ensure this runner has been loaded before calling {nameof(RunTestBlocking)}");

                bool completed = false;
                Exception exception = null;

                void complete()
                {
                    // We want to remove the TestCase from the hierarchy on completion as under nUnit, it may have operations run on it from a different thread.
                    // This is because nUnit will reuse the same class multiple times, running a different [Test] method each time, while the GameHost
                    // is run from its own asynchronous thread.
                    Remove(test);
                    completed = true;
                }

                Schedule(() =>
                {
                    Add(test);

                    Console.WriteLine($@"{(int)Time.Current}: Running {test} visual test cases...");

                    // Nunit will run the tests in the TestCase with the same TestCase instance so the TestCase
                    // needs to be removed before the host is exited, otherwise it will end up disposed

                    test.RunAllSteps(() =>
                    {
                        Scheduler.AddDelayed(complete, time_between_tests);
                    }, e =>
                    {
                        exception = e;
                        complete();
                    });
                });

                while (!completed && host.ExecutionState == ExecutionState.Running)
                    Thread.Sleep(10);

                if (exception != null)
                    throw exception;
            }
        }
    }

    public interface ITestCaseTestRunner
    {
        /// <summary>
        /// Blocks execution until a provided <see cref="TestCase"/> runs to completion.
        /// </summary>
        /// <param name="test">The <see cref="TestCase"/> to run.</param>
        void RunTestBlocking(TestCase test);
    }
}
