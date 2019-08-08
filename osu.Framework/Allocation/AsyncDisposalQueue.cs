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

        private static readonly List<IDisposable> disposal_queue = new List<IDisposable>();

        private static Task runTask;

        public static void Enqueue(IDisposable disposable)
        {
            lock (disposal_queue)
                disposal_queue.Add(disposable);

            if (runTask?.Status < TaskStatus.Running)
                return;

            runTask = Task.Run(() =>
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
            });
        }
    }
}
