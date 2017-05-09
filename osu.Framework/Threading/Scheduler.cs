// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using osu.Framework.Extensions;

namespace osu.Framework.Threading
{
    /// <summary>
    /// Marshals delegates to run from the Scheduler's base thread in a threadsafe manner
    /// </summary>
    public class Scheduler : IDisposable
    {
        private readonly ConcurrentQueue<Action> schedulerQueue = new ConcurrentQueue<Action>();
        private readonly List<ScheduledDelegate> timedTasks = new List<ScheduledDelegate>();
        private readonly List<ScheduledDelegate> perUpdateTasks = new List<ScheduledDelegate>();
        private int mainThreadId;
        private readonly Stopwatch timer = new Stopwatch();

        /// <summary>
        /// The base thread is assumed to be the the thread on which the constructor is run.
        /// </summary>
        public Scheduler()
        {
            SetCurrentThread();
            timer.Start();
        }

        /// <summary>
        /// The base thread is assumed to be the the thread on which the constructor is run.
        /// </summary>
        public Scheduler(Thread mainThread)
        {
            SetCurrentThread(mainThread);
            timer.Start();
        }

        /// <summary>
        /// Returns whether we are on the main thread or not.
        /// </summary>
        protected virtual bool IsMainThread => Thread.CurrentThread.ManagedThreadId == mainThreadId;


        private readonly List<ScheduledDelegate> tasksToSchedule = new List<ScheduledDelegate>();
        private readonly List<ScheduledDelegate> tasksToRemove = new List<ScheduledDelegate>();

        /// <summary>
        /// Run any pending work tasks.
        /// </summary>
        /// <returns>true if any tasks were run.</returns>
        public int Update()
        {
            long currentTime = timer.ElapsedMilliseconds;

            lock (timedTasks)
            {
                if (timedTasks.Count > 0)
                {
                    foreach (var sd in timedTasks)
                    {
                        if (sd.ExecutionTime <= currentTime)
                        {
                            tasksToRemove.Add(sd);

                            if (sd.Cancelled) break;

                            schedulerQueue.Enqueue(sd.RunTask);

                            if (sd.RepeatInterval >= 0)
                            {
                                if (timedTasks.Count > 1000)
                                    throw new ArgumentException("Too many timed tasks are in the queue!");

                                sd.ExecutionTime += sd.RepeatInterval;
                                tasksToSchedule.Add(sd);
                            }
                        }
                    }

                    foreach (var t in tasksToRemove)
                        timedTasks.Remove(t);

                    tasksToRemove.Clear();

                    foreach (var t in tasksToSchedule)
                        timedTasks.AddInPlace(t);

                    tasksToSchedule.Clear();
                }
            }

            for (int i = 0; i < perUpdateTasks.Count; i++)
            {
                ScheduledDelegate task = perUpdateTasks[i];
                if (task.Cancelled)
                {
                    perUpdateTasks.RemoveAt(i--);
                    continue;
                }

                schedulerQueue.Enqueue(task.RunTask);
            }

            int countRun = 0;

            Action action;
            while (schedulerQueue.TryDequeue(out action))
            {
                //todo: error handling
                action.Invoke();
                countRun++;
            }

            return countRun;
        }

        internal void SetCurrentThread(Thread thread)
        {
            mainThreadId = thread?.ManagedThreadId ?? -1;
        }

        internal void SetCurrentThread()
        {
            mainThreadId = Thread.CurrentThread.ManagedThreadId;
        }

        /// <summary>
        /// Add a task to be scheduled.
        /// </summary>
        /// <param name="task">The work to be done.</param>
        /// <param name="forceScheduled">If set to false, the task will be executed immediately if we are on the main thread.</param>
        /// <returns>Whether we could run without scheduling</returns>
        public bool Add(Action task, bool forceScheduled = true)
        {
            if (!forceScheduled && IsMainThread)
            {
                //We are on the main thread already - don't need to schedule.
                task.Invoke();
                return true;
            }

            schedulerQueue.Enqueue(task);

            return false;
        }

        public bool Add(ScheduledDelegate task)
        {
            lock (timedTasks)
            {
                if (task.RepeatInterval == 0)
                    perUpdateTasks.Add(task);
                else
                    timedTasks.AddInPlace(task);
            }
            return true;
        }

        /// <summary>
        /// Add a task which will be run after a specified delay.
        /// </summary>
        /// <param name="task">The work to be done.</param>
        /// <param name="timeUntilRun">Milliseconds until run.</param>
        /// <param name="repeat">Whether this task should repeat.</param>
        public ScheduledDelegate AddDelayed(Action task, double timeUntilRun, bool repeat = false)
        {
            ScheduledDelegate del = new ScheduledDelegate(task, timer.ElapsedMilliseconds + timeUntilRun, repeat ? timeUntilRun : -1);

            return Add(del) ? del : null;
        }

        /// <summary>
        /// Adds a task which will only be run once per frame, no matter how many times it was scheduled in the previous frame.
        /// </summary>
        /// <param name="task">The work to be done.</param>
        /// <returns>Whether this is the first queue attempt of this work.</returns>
        public bool AddOnce(Action task)
        {
            if (schedulerQueue.Contains(task))
                return false;

            schedulerQueue.Enqueue(task);

            return true;
        }

        #region IDisposable Support

        private bool isDisposed; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                isDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion
    }

    public class ScheduledDelegate : IComparable<ScheduledDelegate>
    {
        public ScheduledDelegate(Action task, double executionTime, double repeatInterval = -1)
        {
            ExecutionTime = executionTime;
            RepeatInterval = repeatInterval;
            this.task = task;
        }

        /// <summary>
        /// The work task.
        /// </summary>
        private readonly Action task;

        /// <summary>
        /// Set to true to skip scheduled executions until we are ready.
        /// </summary>
        internal bool Waiting;

        public void Wait()
        {
            Waiting = true;
        }

        public void Continue()
        {
            Waiting = false;
        }

        public void RunTask()
        {
            if (!Waiting)
                task();
            Completed = true;
        }

        public bool Completed;

        public bool Cancelled { get; private set; }

        public void Cancel()
        {
            Cancelled = true;
        }

        /// <summary>
        /// The earliest ElapsedTime value at which we can be executed.
        /// </summary>
        public double ExecutionTime;

        /// <summary>
        /// Time in milliseconds between repeats of this task. -1 means no repeats.
        /// </summary>
        public double RepeatInterval;

        public int CompareTo(ScheduledDelegate other)
        {
            return ExecutionTime == other.ExecutionTime ? -1 : ExecutionTime.CompareTo(other.ExecutionTime);
        }
    }

    /// <summary>
    /// A scheduler which doesn't require manual updates (and never uses the main thread).
    /// </summary>
    public class ThreadedScheduler : Scheduler
    {
        private bool isDisposed;

        public ThreadedScheduler(string threadName = null, int runInterval = 50)
        {
            var workerThread = new Thread(() =>
            {
                while (!isDisposed)
                {
                    Update();
                    Thread.Sleep(runInterval);
                }
            })
            {
                IsBackground = true,
                Name = threadName
            };

            workerThread.Start();
        }

        protected override void Dispose(bool disposing)
        {
            isDisposed = true;
            base.Dispose(disposing);
        }

        protected override bool IsMainThread => false;
    }
}
