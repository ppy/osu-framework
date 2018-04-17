// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Allocation;
using osu.Framework.Configuration;
using osu.Framework.Platform;
using osu.Framework.Screens;

namespace osu.Framework.Testing
{
    public class TestCaseTestRunner : Game
    {
        private readonly TestRunner runner;

        public TestCaseTestRunner()
        {
            Add(runner = new TestRunner());
        }

        public void RunTest(TestCase test) => runner.RunTest(test);

        public class TestRunner : Screen
        {
            private const double time_between_tests = 200;

            private Bindable<double> volume;
            private double volumeAtStartup;

            private GameHost host;

            public TestRunner()
            {
            }

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
                volume.Value = volumeAtStartup;
                base.Dispose(isDisposing);
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();

                host.MaximumDrawHz = int.MaxValue;
                host.MaximumUpdateHz = int.MaxValue;
                host.MaximumInactiveHz = int.MaxValue;
             }

            public void RunTest(TestCase test)
            {
                Add(test);

                Console.WriteLine($@"{(int)Time.Current}: Running {test} visual test cases...");

                // Nunit will run the tests in the TestCase with the same TestCase instance so the TestCase
                // needs to be removed before the host is exited, otherwise it will end up disposed

                test.RunAllSteps(() =>
                {
                    Scheduler.AddDelayed(() =>
                    {
                        Remove(test);
                    }, time_between_tests);
                }, e =>
                {
                    // Other tests may run even if this one failed, so the TestCase still needs to be removed
                    Remove(test);
                    throw new Exception("The test case threw an exception while running", e);
                });
            }
        }
    }
}
