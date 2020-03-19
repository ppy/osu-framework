// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using osu.Framework.Development;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Logging;
using osu.Framework.Threading;

namespace osu.Framework.Platform
{
    /// <summary>
    /// Runs a game host in a specifc threading mode.
    /// </summary>
    public class ThreadRunner
    {
        private readonly InputThread mainThread;

        private readonly List<GameThread> threads = new List<GameThread>();

        public IReadOnlyCollection<GameThread> Threads
        {
            get
            {
                lock (threads)
                    return threads.ToArray();
            }
        }

        private double maximumUpdateHz = GameThread.DEFAULT_ACTIVE_HZ;

        public double MaximumUpdateHz
        {
            set
            {
                maximumUpdateHz = value;
                updateMainThreadRates();
            }
        }

        private double maximumInactiveHz = GameThread.DEFAULT_INACTIVE_HZ;

        public double MaximumInactiveHz
        {
            set
            {
                maximumInactiveHz = value;
                updateMainThreadRates();
            }
        }

        /// <summary>
        /// Construct a new ThreadRunner instance.
        /// </summary>
        /// <param name="mainThread">The main window thread. Used for input in multi-threaded execution; all game logic in single-threaded execution.</param>
        /// <exception cref="NotImplementedException"></exception>
        public ThreadRunner(InputThread mainThread)
        {
            this.mainThread = mainThread;
            AddThread(mainThread);
        }

        /// <summary>
        /// Add a new non-main thread. In single-threaded execution, threads will be executed in the order they are added.
        /// </summary>
        public void AddThread(GameThread thread)
        {
            lock (threads)
            {
                if (!threads.Contains(thread))
                    threads.Add(thread);
            }
        }

        /// <summary>
        /// Remove a non-main thread.
        /// </summary>
        public void RemoveThread(GameThread thread)
        {
            lock (threads)
                threads.Remove(thread);
        }

        private ExecutionMode? activeExecutionMode;

        public ExecutionMode ExecutionMode { private get; set; } = ExecutionMode.MultiThreaded;

        public virtual void RunMainLoop()
        {
            // propagate any requested change in execution mode at a safe point in frame execution
            ensureCorrectExecutionMode();

            Debug.Assert(activeExecutionMode != null);

            switch (activeExecutionMode.Value)
            {
                case ExecutionMode.SingleThread:
                {
                    lock (threads)
                    {
                        foreach (var t in threads)
                            t.ProcessFrame();
                    }

                    break;
                }

                case ExecutionMode.MultiThreaded:
                    // still need to run the main/input thread on the window loop
                    mainThread.ProcessFrame();
                    break;
            }
        }

        public void Start() => ensureCorrectExecutionMode();

        public void Suspend()
        {
            pauseAllThreads();

            // set the active execution mode back to null to set the state checking back to when it can be resumed.
            activeExecutionMode = null;
        }

        public void Stop()
        {
            const int thread_join_timeout = 30000;

            Threads.ForEach(t => t.Exit());
            Threads.Where(t => t.Running).ForEach(t =>
            {
                if (!t.Thread.Join(thread_join_timeout))
                    Logger.Log($"Thread {t.Name} failed to exit in allocated time ({thread_join_timeout}ms).", LoggingTarget.Runtime, LogLevel.Important);
            });

            // as the input thread isn't actually handled by a thread, the above join does not necessarily mean it has been completed to an exiting state.
            while (!mainThread.Exited)
                mainThread.ProcessFrame();

            ThreadSafety.ResetAllForCurrentThread();
        }

        private void ensureCorrectExecutionMode()
        {
            if (ExecutionMode == activeExecutionMode)
                return;

            if (activeExecutionMode == null)
                // in the case we have not yet got an execution mode, set this early to allow usage in GameThread.Initialize overrides.
                activeExecutionMode = ThreadSafety.ExecutionMode = ExecutionMode;

            pauseAllThreads();

            switch (ExecutionMode)
            {
                case ExecutionMode.MultiThreaded:
                {
                    // switch to multi-threaded
                    foreach (var t in Threads)
                    {
                        t.Start();
                        t.Clock.Throttling = true;
                    }

                    break;
                }

                case ExecutionMode.SingleThread:
                {
                    // switch to single-threaded.
                    foreach (var t in Threads)
                    {
                        // only throttle for the main thread
                        t.Initialize(withThrottling: t == mainThread);
                    }

                    // this is usually done in the execution loop, but required here for the initial game startup,
                    // which would otherwise leave values in an incorrect state.
                    ThreadSafety.ResetAllForCurrentThread();
                    break;
                }
            }

            activeExecutionMode = ThreadSafety.ExecutionMode = ExecutionMode;

            updateMainThreadRates();
        }

        private void pauseAllThreads()
        {
            // shut down threads in reverse to ensure audio stops last (other threads may be waiting on a queued event otherwise)
            foreach (var t in Threads.Reverse())
                t.Pause();
        }

        private void updateMainThreadRates()
        {
            if (activeExecutionMode == ExecutionMode.SingleThread)
            {
                mainThread.ActiveHz = maximumUpdateHz;
                mainThread.InactiveHz = maximumInactiveHz;
            }
            else
            {
                mainThread.ActiveHz = GameThread.DEFAULT_ACTIVE_HZ;
                mainThread.InactiveHz = GameThread.DEFAULT_INACTIVE_HZ;
            }
        }
    }
}
