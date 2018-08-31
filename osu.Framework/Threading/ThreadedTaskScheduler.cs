// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace osu.Framework.Threading
{
    /// <summary>
    /// Provides a scheduler that uses a managed thread "pool".
    /// </summary>
    public sealed class ThreadedTaskScheduler : TaskScheduler, IDisposable
    {
        private readonly BlockingCollection<Task> tasks;

        private readonly ImmutableArray<Thread> threads;

        /// <summary>
        /// Initializes a new instance of the StaTaskScheduler class with the specified concurrency level.
        /// </summary>
        /// <param name="numberOfThreads">The number of threads that should be created and used by this scheduler.</param>
        public ThreadedTaskScheduler(int numberOfThreads)
        {
            if (numberOfThreads < 1)
                throw new ArgumentOutOfRangeException(nameof(numberOfThreads));

            tasks = new BlockingCollection<Task>();

            threads = Enumerable.Range(0, numberOfThreads).Select(i =>
            {
                var thread = new Thread(processTasks)
                {
                    Name = "LoadComponentThreadPool",
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
            foreach (var t in tasks.GetConsumingEnumerable()) TryExecuteTask(t);
        }

        /// <summary>
        /// Queues a Task to be executed by this scheduler.
        /// </summary>
        /// <param name="task">The task to be executed.</param>
        protected override void QueueTask(Task task) => tasks.Add(task);

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
        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            return threads.Contains(Thread.CurrentThread) && TryExecuteTask(task);
        }

        /// <summary>Gets the maximum concurrency level supported by this scheduler.</summary>
        public override int MaximumConcurrencyLevel => threads.Length;

        /// <summary>
        /// Cleans up the scheduler by indicating that no more tasks will be queued.
        /// This method blocks until all threads successfully shutdown.
        /// </summary>
        public void Dispose()
        {
            tasks.CompleteAdding();

            foreach (var thread in threads)
                thread.Join();

            tasks.Dispose();
        }
    }
}
