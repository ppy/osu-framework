// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using NUnit.Framework;
using osu.Framework.Logging;
using osu.Framework.Testing;
using osu.Framework.Threading;
using osu.Framework.Timing;

namespace osu.Framework.Tests.Threading
{
    [TestFixture]
    public class SchedulerTest
    {
        private Scheduler scheduler = null!;

        private bool fromMainThread;

        [SetUp]
        public void Setup()
        {
            scheduler = new Scheduler(() => fromMainThread, new StopwatchClock(true));
        }

        [Test]
        public void TestLogOutputFromManyQueuedTasks([Values(false, true)] bool withFlushing)
        {
            int matchingLogCount = 0;

            using (var storage = new TemporaryNativeStorage(nameof(TestLogOutputFromManyQueuedTasks)))
            {
                Logger.Storage = storage;
                Logger.Enabled = true;

                Logger.NewEntry += logTest;

                Assert.AreEqual(0, matchingLogCount);

                for (int i = 0; i < Scheduler.LOG_EXCESSSIVE_QUEUE_LENGTH_INTERVAL / 2; i++)
                {
                    scheduler.Add(() => { });
                    if (withFlushing) scheduler.Update();
                }

                Assert.AreEqual(0, matchingLogCount);

                for (int i = 0; i < Scheduler.LOG_EXCESSSIVE_QUEUE_LENGTH_INTERVAL / 2; i++)
                {
                    scheduler.Add(() => { });
                    if (withFlushing) scheduler.Update();
                }

                Assert.AreEqual(withFlushing ? 0 : 1, matchingLogCount);

                for (int i = 0; i < Scheduler.LOG_EXCESSSIVE_QUEUE_LENGTH_INTERVAL; i++)
                {
                    scheduler.Add(() => { });
                    if (withFlushing) scheduler.Update();
                }

                Assert.AreEqual(withFlushing ? 0 : 2, matchingLogCount);

                Logger.NewEntry -= logTest;
                Logger.Enabled = false;
                Logger.Flush();

                void logTest(LogEntry entry)
                {
                    if (entry.Target == LoggingTarget.Performance && entry.Message.Contains("tasks pending"))
                        matchingLogCount++;
                }
            }
        }

        [Test]
        public void TestLogOutputFromManyQueuedScheduledTasks([Values(false, true)] bool withFlushing)
        {
            int matchingLogCount = 0;

            using (var storage = new TemporaryNativeStorage(nameof(TestLogOutputFromManyQueuedScheduledTasks)))
            {
                Logger.Storage = storage;
                Logger.Enabled = true;

                Logger.NewEntry += logTest;

                Assert.AreEqual(0, matchingLogCount);

                for (int i = 0; i < Scheduler.LOG_EXCESSSIVE_QUEUE_LENGTH_INTERVAL / 2; i++)
                {
                    scheduler.AddDelayed(() => { }, 0);
                    if (withFlushing) scheduler.Update();
                }

                Assert.AreEqual(0, matchingLogCount);

                for (int i = 0; i < Scheduler.LOG_EXCESSSIVE_QUEUE_LENGTH_INTERVAL / 2; i++)
                {
                    scheduler.AddDelayed(() => { }, 0);
                    if (withFlushing) scheduler.Update();
                }

                Assert.AreEqual(withFlushing ? 0 : 1, matchingLogCount);

                for (int i = 0; i < Scheduler.LOG_EXCESSSIVE_QUEUE_LENGTH_INTERVAL; i++)
                {
                    scheduler.AddDelayed(() => { }, 0);
                    if (withFlushing) scheduler.Update();
                }

                Assert.AreEqual(withFlushing ? 0 : 2, matchingLogCount);

                Logger.NewEntry -= logTest;
                Logger.Enabled = false;
                Logger.Flush();

                void logTest(LogEntry entry)
                {
                    if (entry.Target == LoggingTarget.Performance && entry.Message.Contains("tasks pending"))
                        matchingLogCount++;
                }
            }
        }

        [Test]
        public void TestScheduleOnce([Values(false, true)] bool fromMainThread, [Values(false, true)] bool forceScheduled)
        {
            this.fromMainThread = fromMainThread;

            int invocations = 0;

            var del = scheduler.Add(() => invocations++, forceScheduled);

            if (fromMainThread && !forceScheduled)
            {
                Assert.AreEqual(1, invocations);
                Assert.That(del, Is.Null);
            }
            else
            {
                Assert.AreEqual(0, invocations);
                Assert.That(del, Is.Not.Null);
            }

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
        public void TestCancelDelayedDelegateDuringRun()
        {
            var clock = new StopwatchClock();
            scheduler.UpdateClock(clock);

            int invocations = 0;

            ScheduledDelegate? del = null;

            scheduler.Add(del = new ScheduledDelegate(() =>
            {
                invocations++;

                // ReSharper disable once AccessToModifiedClosure
                del?.Cancel();
            }, 1000));

            Assert.AreEqual(ScheduledDelegate.RunState.Waiting, del.State);

            clock.Seek(1500);
            scheduler.Update();
            Assert.AreEqual(1, invocations);

            Assert.AreEqual(ScheduledDelegate.RunState.Cancelled, del.State);

            clock.Seek(2500);
            scheduler.Update();
            Assert.AreEqual(1, invocations);
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

        [Test]
        public void TestZeroDelayRepeatingDelegate()
        {
            var clock = new StopwatchClock();
            scheduler.UpdateClock(clock);

            int invocations = 0;

            Assert.Zero(scheduler.TotalPendingTasks);

            scheduler.Add(new ScheduledDelegate(() => invocations++, 500, 0));

            Assert.AreEqual(1, scheduler.TotalPendingTasks);

            int expectedInvocations = 0;

            for (double d = 0; d <= 2500; d += 100)
            {
                clock.Seek(d);
                scheduler.Update();

                if (d >= 500)
                    expectedInvocations++;

                Assert.AreEqual(expectedInvocations, invocations);
                Assert.AreEqual(1, scheduler.TotalPendingTasks);
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

                int expectedInvocations;

                if (performCatchUp)
                    expectedInvocations = (int)(d / 500);
                else
                    expectedInvocations = (int)(d / 2000);

                Assert.AreEqual(expectedInvocations, invocations);
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

            Assert.AreEqual(ScheduledDelegate.RunState.Waiting, del.State);

            clock.Seek(750);
            scheduler.Update();
            Assert.AreEqual(1, invocations);

            Assert.AreEqual(ScheduledDelegate.RunState.Complete, del.State);

            del.Cancel();

            Assert.AreEqual(ScheduledDelegate.RunState.Cancelled, del.State);

            clock.Seek(1250);
            scheduler.Update();
            Assert.AreEqual(1, invocations);

            Assert.AreEqual(ScheduledDelegate.RunState.Cancelled, del.State);
        }

        private int classInvocations;

        [Test]
        public void TestAddOnceClassMethod()
        {
            classInvocations = 0;

            scheduler.AddOnce(classAction);
            scheduler.AddOnce(classAction);

            scheduler.Update();
            Assert.AreEqual(1, classInvocations);
        }

        private void classAction()
        {
            classInvocations++;
        }

        [Test]
        public void TestAddOnceInlineDelegate()
        {
            classInvocations = 0;

            for (int i = 0; i < 10; i++)
                invokeInlineDelegateAction();

            scheduler.Update();
            Assert.AreEqual(1, classInvocations);
        }

        private void invokeInlineDelegateAction()
        {
            scheduler.AddOnce(() => classInvocations++);
        }

        [Test]
        public void TestAddOnceInlineFunction()
        {
            int invocations = 0;

            void action() => invocations++;

            scheduler.AddOnce(action);
            scheduler.AddOnce(action);

            scheduler.Update();
            Assert.AreEqual(1, invocations);
        }

        [Test]
        public void TestAddOnceInlineFunctionCalledMultipleTimes()
        {
            classInvocations = 0;

            invokeAction();
            invokeAction();

            scheduler.Update();
            Assert.AreEqual(1, classInvocations);
        }

        private void invokeAction()
        {
            void action() => classInvocations++;
            scheduler.AddOnce(action);
        }

        [Test]
        public void TestAddOnceWithDataUsesMostRecentData()
        {
            int receivedData = 0;

            void action(int i) => receivedData = i;

            scheduler.AddOnce(action, 1);
            scheduler.AddOnce(action, 2);

            scheduler.Update();
            Assert.AreEqual(2, receivedData);
        }

        [Test]
        public void TestAddOnceInlineFunctionWithVariable()
        {
            int invocations = 0;

            void action(object o) => invocations++;

            scheduler.AddOnce(action, this);
            scheduler.AddOnce(action, this);

            scheduler.Update();
            Assert.AreEqual(1, invocations);
        }

        [Test]
        public void TestAddOnceDelegateWithVariable()
        {
            int invocations = 0;

            Action<object> action = _ => invocations++;

            scheduler.AddOnce(action, this);
            scheduler.AddOnce(action, this);

            scheduler.Update();
            Assert.AreEqual(1, invocations);
        }

        [Test]
        public void TestPerUpdateTask()
        {
            int invocations = 0;

            scheduler.AddDelayed(() => invocations++, 0, true);
            Assert.AreEqual(0, invocations);

            scheduler.Update();
            Assert.AreEqual(1, invocations);

            scheduler.Update();
            Assert.AreEqual(2, invocations);
        }

        [Test]
        public void TestScheduleFromInsideDelegate([Values(false, true)] bool forceScheduled)
        {
            const int max_reschedules = 3;

            fromMainThread = !forceScheduled;

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

        [Test]
        public void TestInvokeBeforeSchedulerRun()
        {
            int invocations = 0;

            ScheduledDelegate del = new ScheduledDelegate(() => invocations++);

            scheduler.Add(del);
            Assert.AreEqual(0, invocations);

            del.RunTask();
            Assert.AreEqual(1, invocations);

            scheduler.Update();
            Assert.AreEqual(1, invocations);
        }

        [Test]
        public void TestInvokeAfterSchedulerRun()
        {
            int invocations = 0;

            ScheduledDelegate del = new ScheduledDelegate(() => invocations++);

            scheduler.Add(del);
            Assert.AreEqual(0, invocations);

            scheduler.Update();
            Assert.AreEqual(1, invocations);

            Assert.Throws<InvalidOperationException>(del.RunTask);
            Assert.AreEqual(1, invocations);
        }

        [Test]
        public void TestInvokeBeforeScheduleUpdate()
        {
            int invocations = 0;
            ScheduledDelegate del;
            scheduler.Add(del = new ScheduledDelegate(() => invocations++));
            Assert.AreEqual(0, invocations);
            del.RunTask();
            Assert.AreEqual(1, invocations);
            scheduler.Update();
            Assert.AreEqual(1, invocations);
        }

        [Test]
        public void TestRepeatAlreadyCompletedSchedule()
        {
            int invocations = 0;
            var del = new ScheduledDelegate(() => invocations++);
            del.RunTask();
            Assert.AreEqual(1, invocations);
            Assert.Throws<InvalidOperationException>(() => scheduler.Add(del));
            scheduler.Update();
            Assert.AreEqual(1, invocations);
        }

        /// <summary>
        /// Tests that delegates added from inside a scheduled callback don't get executed when the scheduled callback cancels a prior intermediate task.
        ///
        /// Delegate 1 - Added at the start.
        /// Delegate 2 - Added at the start, cancelled by Delegate 1.
        /// Delegate 3 - Added during Delegate 1 callback, should not get executed.
        /// </summary>
        [Test]
        public void TestDelegateAddedInCallbackNotExecutedAfterIntermediateCancelledDelegate()
        {
            int invocations = 0;

            // Delegate 2
            var cancelled = new ScheduledDelegate(() => { });

            // Delegate 1
            scheduler.Add(() =>
            {
                invocations++;

                cancelled.Cancel();

                // Delegate 3
                scheduler.Add(() => invocations++);
            });

            scheduler.Add(cancelled);

            scheduler.Update();
            Assert.That(invocations, Is.EqualTo(1));
        }
    }
}
