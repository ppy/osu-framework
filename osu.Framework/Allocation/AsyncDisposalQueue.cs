// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using osu.Framework.Statistics;

namespace osu.Framework.Allocation
{
    /// <summary>
    /// A queue which batches object disposal on threadpool threads.
    /// </summary>
    internal static class AsyncDisposalQueue
    {
        private static readonly GlobalStatistic<string> last_disposal = GlobalStatistics.Get<string>("Drawable", "Last disposal");

        private static readonly Queue<IDisposable> disposal_queue = new Queue<IDisposable>();

        private static Task runTask;

        public static void Enqueue(IDisposable disposable)
        {
            lock (disposal_queue)
                disposal_queue.Enqueue(disposable);

            if (runTask?.Status < TaskStatus.Running)
                return;

            runTask = Task.Run(() =>
            {
                lock (disposal_queue)
                {
                    while (disposal_queue.Count > 0)
                    {
                        var toDispose = disposal_queue.Dequeue();
                        last_disposal.Value = toDispose.ToString();
                        toDispose.Dispose();
                    }
                }
            });
        }
    }
}
