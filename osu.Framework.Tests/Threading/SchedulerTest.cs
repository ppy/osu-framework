// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Threading;
using NUnit.Framework;
using osu.Framework.Threading;
using osu.Framework.Timing;

namespace osu.Framework.Tests.Threading
{
    [TestFixture]
    public class SchedulerTest
    {
        private Scheduler scheduler;

        [SetUp]
        public void Setup()
        {
            scheduler = new Scheduler(new Thread(() => { }));
        }

        [Test]
        public void TestScheduleOnce([Values(false, true)] bool fromMainThread, [Values(false, true)] bool forceScheduled)
        {
            if (fromMainThread)
                scheduler.SetCurrentThread();

            int invocations = 0;

            scheduler.Add(() => invocations++, forceScheduled);

            if (fromMainThread && !forceScheduled)
                Assert.AreEqual(1, invocations);
            else
                Assert.AreEqual(0, invocations);

            scheduler.Update();
            Assert.AreEqual(1, invocations);

            // Ensures that the delegate runs only once in the scheduled/not on main thread branch
            scheduler.Update();
            Assert.AreEqual(1, invocations);
        }

        [Test]
        public void TestCancelScheduledDelegate()
        {
            int invocations = 0;

            ScheduledDelegate del;
            scheduler.Add(del = new ScheduledDelegate(() => invocations++));

            del.Cancel();

            scheduler.Update();
            Assert.AreEqual(0, invocations);
        }

        [Test]
        public void TestAddCancelledDelegate()
        {
            int invocations = 0;

            ScheduledDelegate del = new ScheduledDelegate(() => invocations++);
            del.Cancel();

            scheduler.Add(del);

            scheduler.Update();
            Assert.AreEqual(0, invocations);
        }

        [Test]
        public void TestCustomScheduledDelegateRunsOnce()
        {
            int invocations = 0;

            scheduler.Add(new ScheduledDelegate(() => invocations++));

            scheduler.Update();
            Assert.AreEqual(1, invocations);

            scheduler.Update();
            Assert.AreEqual(1, invocations);
        }

        [Test]
        public void TestDelayedDelegatesAreScheduledWithCorrectTime()
        {
            var clock = new StopwatchClock();
            scheduler.UpdateClock(clock);

            ScheduledDelegate del = scheduler.AddDelayed(() => { }, 1000);

            Assert.AreEqual(1000, del.ExecutionTime);

            clock.Seek(500);
            del = scheduler.AddDelayed(() => { }, 1000);

            Assert.AreEqual(1500, del.ExecutionTime);
        }

        [Test]
        public void TestDelayedDelegateDoesNotRunUntilExpectedTime()
        {
            var clock = new StopwatchClock();
            scheduler.UpdateClock(clock);

            int invocations = 0;

            scheduler.AddDelayed(() => invocations++, 1000);

            clock.Seek(1000);
            scheduler.AddDelayed(() => invocations++, 1000);

            for (double d = 0; d <= 2500; d += 100)
            {
                clock.Seek(d);
                scheduler.Update();

                int expectedInvocations = 0;

                if (d >= 1000)
                    expectedInvocations++;
                if (d >= 2000)
                    expectedInvocations++;

                Assert.AreEqual(expectedInvocations, invocations);
            }
        }

        [Test]
        public void TestCancelDelayedDelegate()
        {
            var clock = new StopwatchClock();
            scheduler.UpdateClock(clock);

            int invocations = 0;

            ScheduledDelegate del;
            scheduler.Add(del = new ScheduledDelegate(() => invocations++, 1000));
            del.Cancel();

            clock.Seek(1500);
            scheduler.Update();
            Assert.AreEqual(0, invocations);
        }

        [Test]
        public void TestRepeatingDelayedDelegate()
        {
            var clock = new StopwatchClock();
            scheduler.UpdateClock(clock);

            int invocations = 0;

            scheduler.Add(new ScheduledDelegate(() => invocations++, 500, 500));
            scheduler.Add(new ScheduledDelegate(() => invocations++, 1000, 500));

            for (double d = 0; d <= 2500; d += 100)
            {
                clock.Seek(d);
                scheduler.Update();

                int expectedInvocations = 0;

                if (d >= 500)
                    expectedInvocations += 1 + (int)((d - 500) / 500);
                if (d >= 1000)
                    expectedInvocations += 1 + (int)((d - 1000) / 500);

                Assert.AreEqual(expectedInvocations, invocations);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TestRepeatingDelayedDelegateCatchUp(bool performCatchUp)
        {
            var clock = new StopwatchClock();
            scheduler.UpdateClock(clock);

            int invocations = 0;

            scheduler.Add(new ScheduledDelegate(() => invocations++, 500, 500)
            {
                PerformRepeatCatchUpExecutions = performCatchUp
            });

            for (double d = 0; d <= 10000; d += 2000)
            {
                clock.Seek(d);

                for (int i = 0; i < 10; i++)
                    // allow catch-up to potentially occur.
                    scheduler.Update();

                int expectedInovations;

                if (performCatchUp)
                    expectedInovations = (int)(d / 500);
                else
                    expectedInovations = (int)(d / 2000);

                Assert.AreEqual(expectedInovations, invocations);
            }
        }

        [Test]
        public void TestCancelAfterRepeatReschedule()
        {
            var clock = new StopwatchClock();
            scheduler.UpdateClock(clock);

            int invocations = 0;

            ScheduledDelegate del;
            scheduler.Add(del = new ScheduledDelegate(() => invocations++, 500, 500));

            clock.Seek(750);
            scheduler.Update();
            Assert.AreEqual(1, invocations);

            del.Cancel();
            clock.Seek(1250);
            scheduler.Update();
            Assert.AreEqual(1, invocations);
        }

        [Test]
        public void TestAddOnce()
        {
            int invocations = 0;

            void action() => invocations++;

            scheduler.AddOnce(action);
            scheduler.AddOnce(action);

            scheduler.Update();
            Assert.AreEqual(1, invocations);
        }

        [Test]
        public void TestScheduleFromInsideDelegate([Values(false, true)] bool forceScheduled)
        {
            const int max_reschedules = 3;

            if (!forceScheduled)
                scheduler.SetCurrentThread();

            int reschedules = 0;

            scheduleTask();

            if (forceScheduled)
            {
                for (int i = 0; i <= max_reschedules; i++)
                {
                    scheduler.Update();
                    Assert.AreEqual(Math.Min(max_reschedules, i + 1), reschedules);
                }
            }
            else
                Assert.AreEqual(max_reschedules, reschedules);

            void scheduleTask() => scheduler.Add(() =>
            {
                if (reschedules == max_reschedules)
                    return;

                reschedules++;
                scheduleTask();
            }, forceScheduled);
        }
    }
}
