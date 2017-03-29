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

            Console.WriteLine($@"{Time}: Running {browser.TestCount} tests for {time_per_action}ms each...");

            runNext();
        }

        private int testIndex = -1;
        private int actionIndex = -1;

        private TestCase currentTest => testIndex >= 0 ? browser.Tests.Skip(testIndex).FirstOrDefault() : null;

        private TestBrowser browser;

        private void runNext()
        {
            if (currentTest != null && actionIndex < currentTest.ButtonsContainer.Children.Count() - 1)
            {
                actionIndex++;
                Console.WriteLine($@"{Time}: Switching to test #{testIndex + 1}-{actionIndex + 1}");
                currentTest.ButtonsContainer.Children.Skip(actionIndex).First().TriggerClick();
            }
            else
            {
                testIndex++;
                if (currentTest != null)
                {
                    browser.LoadTest(currentTest);
                    actionIndex = -1;
                }
                else
                {
                    //we're done
                    Scheduler.AddDelayed(Host.Exit, time_per_action);
                    return;
                }
            }

            Scheduler.AddDelayed(runNext, time_per_action);
        }
    }
}
