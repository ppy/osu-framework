// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Linq;
using osu.Framework.Testing;

namespace osu.Framework.VisualTests
{
    public class Benchmark : Game
    {
        private const double time_per_action = 200;

        protected override void LoadComplete()
        {
            base.LoadComplete();

            Host.MaximumDrawHz = int.MaxValue;
            Host.MaximumUpdateHz = int.MaxValue;
            Host.MaximumInactiveHz = int.MaxValue;

            browser = new TestBrowser();
            Add(browser);

            Console.WriteLine($@"{Time}: Running {browser.Tests.Count} tests using a delay of {time_per_action}ms per action...");

            runNext();
        }

        private int testIndex;
        private int actionIndex = -1;
        private int actionRepetition;

        private TestCase loadableTest => testIndex >= 0 ? browser.Tests.Skip(testIndex).FirstOrDefault() : null;
        private StepButton loadableStep => actionIndex >= 0 ? loadableTest?.StepsContainer.Children.Skip(actionIndex).FirstOrDefault() : null;

        private TestBrowser browser;

        private void runNext()
        {
            if (loadableTest == null)
            {
                //we're done
                Scheduler.AddDelayed(Host.Exit, time_per_action);
                return;
            }

            if (browser.CurrentTest != loadableTest)
                browser.LoadTest(loadableTest);

            Console.WriteLine($@"{Time.Current:N0}: running test {testIndex + 1}.{actionIndex + 1}.{actionRepetition}");
            actionRepetition++;
            loadableStep?.TriggerClick();

            if (actionRepetition > (loadableStep?.RequiredRepetitions ?? 1) - 1)
            {
                actionIndex++;
                actionRepetition = 0;
            }

            if (actionIndex > loadableTest.StepsContainer.Children.Count() - 1)
            {
                testIndex++;
                actionRepetition = 0;
                actionIndex = -1;
            }

            Scheduler.AddDelayed(runNext, time_per_action);
        }
    }
}
