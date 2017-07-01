// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Platform;
using osu.Framework.Screens;

namespace osu.Framework.Testing
{
    public class TestRunner : Screen
    {
        private const double time_between_tests = 200;

        public TestRunner(TestBrowser browser)
        {
            this.browser = browser;
        }

        [BackgroundDependencyLoader]
        private void load(GameHost host)
        {
            this.host = host;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            host.MaximumDrawHz = int.MaxValue;
            host.MaximumUpdateHz = int.MaxValue;
            host.MaximumInactiveHz = int.MaxValue;

            Push(browser);

            Console.WriteLine($@"{(int)Time.Current}: Running {browser.Tests.Count} visual test cases...");

            runNext();
        }

        private int testIndex;

        private TestCase loadableTest => testIndex >= 0 ? browser.Tests.ElementAtOrDefault(testIndex) : null;

        private readonly TestBrowser browser;
        private GameHost host;

        private void runNext()
        {
            if (loadableTest == null)
            {
                //we're done
                Scheduler.AddDelayed(host.Exit, time_between_tests);
                return;
            }

            if (browser.CurrentTest != loadableTest)
                browser.LoadTest(loadableTest, () =>
                {
                    testIndex++;
                    Scheduler.AddDelayed(runNext, time_between_tests);
                });
        }
    }
}
