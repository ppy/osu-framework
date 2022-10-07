// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using osu.Framework.Threading;

namespace osu.Framework.Tests.Threading
{
    [TestFixture]
    public class ThreadedTaskSchedulerTest
    {
        /// <summary>
        /// On disposal, <see cref="ThreadedTaskScheduler"/> does a blocking shutdown sequence.
        /// This asserts all outstanding tasks are run before the shutdown completes.
        /// </summary>
        [Test]
        public void EnsureThreadedTaskSchedulerProcessesBeforeDispose()
        {
            int runCount = 0;

            const int target_count = 128;

            var taskScheduler = new ThreadedTaskScheduler(4, "test");

            using (taskScheduler)
            {
                for (int i = 0; i < target_count; i++)
                {
                    Task.Factory.StartNew(() =>
                    {
                        Interlocked.Increment(ref runCount);
                        Thread.Sleep(100);
                    }, default, TaskCreationOptions.HideScheduler, taskScheduler);
                }
            }

            // test against double disposal crashes.
            taskScheduler.Dispose();

            Assert.AreEqual(target_count, runCount);
        }

        [Test]
        public void EnsureEventualDisposalWithStuckTasks()
        {
            ManualResetEventSlim exited = new ManualResetEventSlim();

            Task.Run(() =>
            {
                using (var taskScheduler = new ThreadedTaskScheduler(4, "test"))
                {
                    Task.Factory.StartNew(() =>
                    {
                        while (!exited.IsSet)
                            Thread.Sleep(100);
                    }, default, TaskCreationOptions.HideScheduler, taskScheduler);
                }

                exited.Set();
            });

            Assert.That(exited.Wait(30000));
        }

        [Test]
        public void EnsureScheduledTaskReturnsOnDisposal()
        {
            ManualResetEventSlim exited = new ManualResetEventSlim();
            ManualResetEventSlim run = new ManualResetEventSlim();
            ThreadedTaskScheduler taskScheduler = new ThreadedTaskScheduler(4, "test");

            taskScheduler.Dispose();

            Task.Run(async () =>
            {
                // ReSharper disable once AccessToDisposedClosure
                await Task.Factory.StartNew(() => { run.Set(); }, default, TaskCreationOptions.HideScheduler, taskScheduler).ConfigureAwait(false);
                exited.Set();
            });

            Assert.That(run.Wait(30000));
            Assert.That(exited.Wait(30000));
        }
    }
}
