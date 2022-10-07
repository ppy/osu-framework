// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using osu.Framework.Development;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Logging;
using osu.Framework.Threading;

namespace osu.Framework.Platform
{
    /// <summary>
    /// Runs a game host in a specific threading mode.
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

        private readonly object startStopLock = new object();

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
                            t.RunSingleFrame();
                    }

                    break;
                }

                case ExecutionMode.MultiThreaded:
                    // still need to run the main/input thread on the window loop
                    mainThread.RunSingleFrame();
                    break;
            }

            ThreadSafety.ResetAllForCurrentThread();
        }

        public void Start() => ensureCorrectExecutionMode();

        public void Suspend()
        {
            lock (startStopLock)
            {
                pauseAllThreads();
                activeExecutionMode = null;
            }
        }

        public void Stop()
        {
            const int thread_join_timeout = 30000;

            // exit in reverse order so AudioThread is exited last (UpdateThread depends on AudioThread)
            Threads.Reverse().ForEach(t =>
            {
                // save the native thread to a local variable as Thread gets set to null when exiting.
                // WaitForState(Exited) appears to be unsafe in multithreaded.
                var thread = t.Thread;

                t.Exit();

                if (thread != null)
                {
                    if (!thread.Join(thread_join_timeout))
                        throw new TimeoutException($"Thread {t.Name} failed to exit in allocated time ({thread_join_timeout}ms).");
                }
                else
                {
                    t.WaitForState(GameThreadState.Exited);
                }

                Debug.Assert(t.Exited);
            });

            ThreadSafety.ResetAllForCurrentThread();
        }

        private void ensureCorrectExecutionMode()
        {
            // locking is required as this method may be called from two different threads.
            lock (startStopLock)
            {
                // pull into a local variable as the property is not locked during writes.
                var executionMode = ExecutionMode;

                if (executionMode == activeExecutionMode)
                    return;

                activeExecutionMode = ThreadSafety.ExecutionMode = executionMode;
                Logger.Log($"Execution mode changed to {activeExecutionMode}");
            }

            pauseAllThreads();

            switch (activeExecutionMode)
            {
                case ExecutionMode.MultiThreaded:
                {
                    // switch to multi-threaded
                    foreach (var t in Threads)
                        t.Start();

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

        /// <summary>
        /// Sets the current culture of all threads to the supplied <paramref name="culture"/>.
        /// </summary>
        public void SetCulture(CultureInfo culture)
        {
            // for single-threaded mode, switch the current (assumed to be main) thread's culture, since it's actually the one that's running the frames.
            Thread.CurrentThread.CurrentCulture = culture;

            // for multi-threaded mode, schedule the culture change on all threads.
            // note that if the threads haven't been created yet (e.g. if the game started single-threaded), this will only store the culture in GameThread.CurrentCulture.
            // in that case, the stored value will be set on the actual threads after the next Start() call.
            foreach (var t in Threads)
            {
                t.Scheduler.Add(() => t.CurrentCulture = culture);
            }
        }
    }
}
