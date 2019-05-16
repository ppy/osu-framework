// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace osu.Framework.Allocation
{
    /// <summary>
    /// A queue which batches object disposal on threadpool threads.
    /// </summary>
    internal static class AsyncDisposalQueue
    {
        private static readonly ConcurrentQueue<IDisposable> disposal_queue = new ConcurrentQueue<IDisposable>();
        private static readonly object task_lock = new object();

        private static Task runTask;

        public static void Enqueue(IDisposable disposable)
        {
            disposal_queue.Enqueue(disposable);

            lock (task_lock)
            {
                if (runTask != null
                    && (runTask.Status == TaskStatus.WaitingForActivation
                        || runTask.Status == TaskStatus.WaitingToRun
                        || runTask.Status == TaskStatus.Created))
                {
                    return;
                }

                runTask = Task.Run(() =>
                {
                    while (disposal_queue.TryDequeue(out var toDispose))
                        toDispose.Dispose();
                });
            }
        }
    }
}
