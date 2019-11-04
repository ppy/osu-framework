// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Concurrent;
using System.Threading;
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

        private static readonly ConcurrentQueue<object> disposal_queue = new ConcurrentQueue<object>();

        private static readonly SemaphoreSlim semaphore = new SemaphoreSlim(0);

        private static readonly Lazy<Task> lazy_task = new Lazy<Task>(disposeLoopAsync, LazyThreadSafetyMode.ExecutionAndPublication);

        public static void Enqueue(IDisposable disposable) => enqueueCore(disposable);

        public static void Enqueue(IAsyncDisposable asyncDisposable) => enqueueCore(asyncDisposable);

        private static void enqueueCore(object disposable)
        {
            disposal_queue.Enqueue(disposable);
            semaphore.Release();
            // ReSharper disable once AssignmentIsFullyDiscarded
            _ = lazy_task.Value; // Touch the value to do a thread-safe creation.
        }

        private static async Task disposeLoopAsync()
        {
            await Task.Yield();

            while (true)
            {
                // The semaphore count should be not more than the queue's count.
                // See implementation of System.Collections.Concurrent.BlockingCollection<T>.
                await semaphore.WaitAsync();
                if (!disposal_queue.TryDequeue(out object obj))
                    throw new InvalidOperationException("Thread safety is broken somewhere.");

                last_disposal.Value = obj.ToString();

                switch (obj)
                {
                    case IAsyncDisposable asyncDisposable:
                        await asyncDisposable.DisposeAsync();
                        break;

                    case IDisposable disposable:
                        disposable.Dispose();
                        break;
                }
            }
        }
    }
}
