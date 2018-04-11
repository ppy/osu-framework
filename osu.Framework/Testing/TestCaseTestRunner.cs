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
        public TestCaseTestRunner(TestCase testCase)
        {
            Add(new TestRunner(testCase));
        }

        public class TestRunner : Screen
        {
            private const double time_between_tests = 200;

            private Bindable<double> volume;
            private double volumeAtStartup;

            private readonly TestCase test;
            private GameHost host;

            public TestRunner(TestCase test)
            {
                this.test = test;
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

                Add(test);

                Console.WriteLine($@"{(int)Time.Current}: Running {test} visual test cases...");

                // Nunit will run the tests in the TestCase with the same TestCase instance so the TestCase
                // needs to be removed before the host is exited, otherwise it will end up disposed

                test.RunAllSteps(() =>
                {
                    Scheduler.AddDelayed(() =>
                    {
                        Remove(test);
                        host.Exit();
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
