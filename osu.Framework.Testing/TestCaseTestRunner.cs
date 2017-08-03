// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
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
        public TestCaseTestRunner(TestCase t)
        {
            Add(new TestRunner(t));
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

                test.RunAllSteps(() =>
                {
                    Scheduler.AddDelayed(host.Exit, time_between_tests);
                });
            }
        }
    }
}