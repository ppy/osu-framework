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

            using (var taskScheduler = new ThreadedTaskScheduler(4, "test"))
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

            Assert.AreEqual(target_count, runCount);
        }
    }
}
