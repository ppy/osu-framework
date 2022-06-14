// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using osu.Framework.Statistics;
using osu.Framework.Threading;

namespace osu.Framework.Allocation
{
    /// <summary>
    /// A queue which batches object disposal on locally managed threads.
    /// </summary>
    internal static class AsyncDisposalQueue
    {
        private static readonly GlobalStatistic<string> last_disposal = GlobalStatistics.Get<string>("Drawable", "Last disposal");

        private static Task runTask;

        private static readonly List<IDisposable> disposal_queue = new List<IDisposable>();

        private static readonly ManualResetEventSlim processing_reset_event = new ManualResetEventSlim(true);

        private static readonly ThreadedTaskScheduler scheduler = new ThreadedTaskScheduler(1, "Disposal queue");

        private static int runningTasks;

        /// <summary>
        /// Enqueue a disposable object for asynchronous disposal.
        /// </summary>
        /// <param name="disposable">The object to dispose.</param>
        public static void Enqueue(IDisposable disposable)
        {
            lock (disposal_queue)
            {
                disposal_queue.Add(disposable);

                if (runTask?.Status < TaskStatus.Running)
                    return;

                Interlocked.Increment(ref runningTasks);
                processing_reset_event.Reset();
            }

            runTask = Task.Factory.StartNew(() =>
            {
                try
                {
                    IDisposable[] itemsToDispose;

                    lock (disposal_queue)
                    {
                        itemsToDispose = disposal_queue.ToArray();
                        disposal_queue.Clear();
                    }

                    for (int i = 0; i < itemsToDispose.Length; i++)
                    {
                        ref var item = ref itemsToDispose[i];

                        last_disposal.Value = item.ToString();

                        item.Dispose();
                        item = null;
                    }
                }
                finally
                {
                    lock (disposal_queue)
                    {
                        if (Interlocked.Decrement(ref runningTasks) == 0)
                            processing_reset_event.Set();
                    }
                }
            }, CancellationToken.None, TaskCreationOptions.None, scheduler);
        }

        /// <summary>
        /// Wait until all items in the async disposal queue have been flushed.
        /// Will wait for a maximum of 10 seconds.
        /// </summary>
        public static void WaitForEmpty() => processing_reset_event.Wait(TimeSpan.FromSeconds(10));
    }
}
