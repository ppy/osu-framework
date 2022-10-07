// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using osu.Framework.Logging;

namespace osu.Framework.Threading
{
    /// <summary>
    /// Provides a scheduler that uses a managed thread "pool".
    /// </summary>
    public sealed class ThreadedTaskScheduler : TaskScheduler, IDisposable
    {
        private readonly BlockingCollection<Task> tasks;

        private readonly ImmutableArray<Thread> threads;

        private readonly string name;

        private bool isDisposed;

        private int runningTaskCount;

        public string GetStatusString() => $"{name} concurrency:{MaximumConcurrencyLevel} running:{runningTaskCount} pending:{pendingTaskCount}";

        /// <summary>
        /// Initializes a new instance of the StaTaskScheduler class with the specified concurrency level.
        /// </summary>
        /// <param name="numberOfThreads">The number of threads that should be created and used by this scheduler.</param>
        /// <param name="name">The thread name to give threads in this pool.</param>
        public ThreadedTaskScheduler(int numberOfThreads, string name)
        {
            if (numberOfThreads < 1)
                throw new ArgumentOutOfRangeException(nameof(numberOfThreads));

            this.name = name;
            tasks = new BlockingCollection<Task>();

            threads = Enumerable.Range(0, numberOfThreads).Select(_ =>
            {
                var thread = new Thread(processTasks)
                {
                    Name = $"{nameof(ThreadedTaskScheduler)} ({name})",
                    IsBackground = true
                };

                thread.Start();

                return thread;
            }).ToImmutableArray();
        }

        /// <summary>
        /// Continually get the next task and try to execute it.
        /// This will continue as a blocking operation until the scheduler is disposed and no more tasks remain.
        /// </summary>
        private void processTasks()
        {
            try
            {
                foreach (var t in tasks.GetConsumingEnumerable())
                {
                    Interlocked.Increment(ref runningTaskCount);
                    TryExecuteTask(t);
                    Interlocked.Decrement(ref runningTaskCount);
                }
            }
            catch (ObjectDisposedException)
            {
                // tasks may have been disposed. there's no easy way to check on this other than catch for it.
            }
        }

        /// <summary>
        /// Queues a Task to be executed by this scheduler.
        /// </summary>
        /// <param name="task">The task to be executed.</param>
        protected override void QueueTask(Task task)
        {
            try
            {
                tasks.Add(task);
            }
            catch (ObjectDisposedException)
            {
                // tasks may have been disposed. there's no easy way to check on this other than catch for it.
                Logger.Log($"Task was queued for execution on a {nameof(ThreadedTaskScheduler)} ({name}) after it was disposed. The task will be executed inline.");
                TryExecuteTask(task);
            }
        }

        /// <summary>
        /// Provides a list of the scheduled tasks for the debugger to consume.
        /// </summary>
        /// <returns>An enumerable of all tasks currently scheduled.</returns>
        protected override IEnumerable<Task> GetScheduledTasks() => tasks.ToArray();

        /// <summary>
        /// Determines whether a Task may be inlined.
        /// </summary>
        /// <param name="task">The task to be executed.</param>
        /// <param name="taskWasPreviouslyQueued">Whether the task was previously queued.</param>
        /// <returns>true if the task was successfully inlined; otherwise, false.</returns>
        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued) => threads.Contains(Thread.CurrentThread) && TryExecuteTask(task);

        /// <summary>Gets the maximum concurrency level supported by this scheduler.</summary>
        public override int MaximumConcurrencyLevel => threads.Length;

        private int pendingTaskCount
        {
            get
            {
                try
                {
                    return tasks.Count;
                }
                catch (ObjectDisposedException)
                {
                    // tasks may have been disposed. there's no easy way to check on this other than catch for it.
                    return 0;
                }
            }
        }

        /// <summary>
        /// Cleans up the scheduler by indicating that no more tasks will be queued.
        /// This method blocks until all threads successfully shutdown.
        /// </summary>
        public void Dispose()
        {
            if (isDisposed)
                return;

            isDisposed = true;

            tasks.CompleteAdding();

            foreach (var thread in threads)
                thread.Join(TimeSpan.FromSeconds(10));

            tasks.Dispose();
        }
    }
}
