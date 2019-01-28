﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using osu.Framework.Extensions;
using osu.Framework.Timing;

namespace osu.Framework.Threading
{
    /// <summary>
    /// Marshals delegates to run from the Scheduler's base thread in a threadsafe manner
    /// </summary>
    public class Scheduler
    {
        private readonly ConcurrentQueue<ScheduledDelegate> runQueue = new ConcurrentQueue<ScheduledDelegate>();
        private readonly List<ScheduledDelegate> timedTasks = new List<ScheduledDelegate>();
        private readonly List<ScheduledDelegate> perUpdateTasks = new List<ScheduledDelegate>();
        private int mainThreadId;

        private IClock clock;
        private double currentTime => clock?.CurrentTime ?? 0;

        /// <summary>
        /// Whether there are any tasks queued to run (including delayed tasks in the future).
        /// </summary>
        public bool HasPendingTasks => !runQueue.IsEmpty || timedTasks.Count > 0 || perUpdateTasks.Count > 0;

        /// <summary>
        /// The base thread is assumed to be the the thread on which the constructor is run.
        /// </summary>
        public Scheduler()
        {
            SetCurrentThread();
            clock = new StopwatchClock(true);
        }

        /// <summary>
        /// The base thread is assumed to be the the thread on which the constructor is run.
        /// </summary>
        public Scheduler(Thread mainThread)
        {
            SetCurrentThread(mainThread);
            clock = new StopwatchClock(true);
        }

        /// <summary>
        /// The base thread is assumed to be the the thread on which the constructor is run.
        /// </summary>
        public Scheduler(Thread mainThread, IClock clock)
        {
            SetCurrentThread(mainThread);
            this.clock = clock;
        }

        public void UpdateClock(IClock newClock)
        {
            if (newClock == null)
                throw new NullReferenceException($"{nameof(newClock)} may not be null.");

            if (newClock == clock)
                return;

            lock (timedTasks)
            {
                if (clock == null)
                {
                    // This is the first time we will get a valid time, so assume this is the
                    // reference point everything scheduled so far starts from.
                    foreach (var s in timedTasks)
                        s.ExecutionTime += newClock.CurrentTime;
                }

                clock = newClock;
            }
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
        public virtual int Update()
        {
            lock (timedTasks)
            {
                double currentTimeLocal = currentTime;

                if (timedTasks.Count > 0)
                {
                    foreach (var sd in timedTasks)
                    {
                        if (sd.ExecutionTime <= currentTimeLocal)
                        {
                            tasksToRemove.Add(sd);

                            if (sd.Cancelled) continue;

                            runQueue.Enqueue(sd);

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

                runQueue.Enqueue(task);
            }

            int countRun = 0;

            while (runQueue.TryDequeue(out ScheduledDelegate sd))
            {
                if (sd.Cancelled) continue;

                //todo: error handling
                sd.RunTask();
                countRun++;
            }

            return countRun;
        }

        /// <summary>
        /// Cancel any pending work tasks.
        /// </summary>
        public void CancelDelayedTasks()
        {
            lock (timedTasks)
            {
                foreach (var t in timedTasks)
                    t.Cancel();
                timedTasks.Clear();
            }
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

            runQueue.Enqueue(new ScheduledDelegate(task));

            return false;
        }

        public void Add(ScheduledDelegate task)
        {
            lock (timedTasks)
            {
                if (task.RepeatInterval == 0)
                    perUpdateTasks.Add(task);
                else
                    timedTasks.AddInPlace(task);
            }
        }

        /// <summary>
        /// Add a task which will be run after a specified delay.
        /// </summary>
        /// <param name="task">The work to be done.</param>
        /// <param name="timeUntilRun">Milliseconds until run.</param>
        /// <param name="repeat">Whether this task should repeat.</param>
        public ScheduledDelegate AddDelayed(Action task, double timeUntilRun, bool repeat = false)
        {
            // We are locking here already to make sure we have no concurrent access to currentTime
            lock (timedTasks)
            {
                ScheduledDelegate del = new ScheduledDelegate(task, currentTime + timeUntilRun, repeat ? timeUntilRun : -1);
                Add(del);
                return del;
            }
        }

        /// <summary>
        /// Adds a task which will only be run once per frame, no matter how many times it was scheduled in the previous frame.
        /// </summary>
        /// <param name="task">The work to be done.</param>
        /// <returns>Whether this is the first queue attempt of this work.</returns>
        public bool AddOnce(Action task)
        {
            if (runQueue.Any(sd => sd.RunTask == task))
                return false;

            runQueue.Enqueue(new ScheduledDelegate(task));

            return true;
        }
    }

    public class ScheduledDelegate : IComparable<ScheduledDelegate>
    {
        public ScheduledDelegate(Action task, double executionTime = 0, double repeatInterval = -1)
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
            if (Cancelled)
                throw new InvalidOperationException($"Can not run a {nameof(ScheduledDelegate)} that has been {nameof(Cancelled)}");

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
}
