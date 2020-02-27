// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using osu.Framework.Development;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Logging;
using osu.Framework.Threading;

namespace osu.Framework.Platform
{
    /// <summary>
    /// Runs a game host in a specifc threading mode.
    /// </summary>
    internal class ThreadRunner
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

        private double maximumUpdateHz;

        public double MaximumUpdateHz
        {
            set
            {
                maximumUpdateHz = value;
                updateMainThreadRates();
            }
        }

        private double maximumInactiveHz;

        public double MaximumInactiveHz
        {
            set
            {
                maximumInactiveHz = value;
                updateMainThreadRates();
            }
        }

        private bool singleThreaded;

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

        public bool SingleThreaded
        {
            get => singleThreaded;
            set
            {
                if (value == singleThreaded) return;

                lock (runStateLock)
                {
                    if (isRunning)
                    {
                        mainThread.Scheduler.Add(() => setRunMode(value));
                    }
                    else
                    {
                        setRunMode(value);
                    }
                }
            }
        }

        private void setRunMode(bool value)
        {
            lock (runStateLock)
            {
                if (!value)
                {
                    foreach (var t in Threads)
                    {
                        if (isRunning) t.Start();
                        t.Clock.Throttling = true;
                    }
                }
                else
                {
                    foreach (var t in Threads)
                    {
                        if (isRunning) t.Pause();
                        t.Clock.Throttling = t == mainThread;
                    }

                    if (isRunning)
                        while (Threads.Any(t => t.Running))
                            Thread.Sleep(1);
                }

                singleThreaded = value;
                ThreadSafety.SingleThreadThread = singleThreaded ? Thread.CurrentThread : null;
                updateMainThreadRates();
            }
        }

        public void RunMainLoop()
        {
            mainThread.ProcessFrame();

            if (singleThreaded)
            {
                lock (threads)
                {
                    foreach (var t in threads)
                    {
                        if (t == mainThread)
                            continue;

                        t.ProcessFrame();
                    }
                }
            }
        }

        private readonly object runStateLock = new object();

        private bool isRunning;

        public void Start()
        {
            lock (runStateLock)
            {
                isRunning = true;

                if (singleThreaded)
                {
                    foreach (var t in Threads)
                    {
                        t.OnThreadStart?.Invoke();
                        t.OnThreadStart = null;
                    }
                }
                else
                {
                    foreach (var t in Threads)
                        t.Start();
                }
            }
        }

        private const int thread_join_timeout = 30000;

        public void Stop()
        {
            lock (runStateLock)
            {
                Threads.ForEach(t => t.Exit());
                Threads.Where(t => t.Running).ForEach(t =>
                {
                    if (!t.Thread.Join(thread_join_timeout))
                        Logger.Log($"Thread {t.Name} failed to exit in allocated time ({thread_join_timeout}ms).", LoggingTarget.Runtime, LogLevel.Important);
                });

                // as the input thread isn't actually handled by a thread, the above join does not necessarily mean it has been completed to an exiting state.
                while (!mainThread.Exited)
                    mainThread.ProcessFrame();

                isRunning = false;
            }
        }

        private void updateMainThreadRates()
        {
            if (singleThreaded)
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
