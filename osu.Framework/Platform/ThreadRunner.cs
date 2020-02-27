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

                mainThread.Scheduler.Add(() =>
                {
                    lock (threads)
                    {
                        if (!value)
                        {
                            foreach (var t in threads)
                            {
                                t.Start();
                                t.Clock.Throttling = true;
                            }
                        }
                        else
                        {
                            foreach (var t in threads)
                            {
                                t.Pause();
                                t.Clock.Throttling = t == mainThread;
                            }

                            while (threads.Any(t => t.Running))
                                Thread.Sleep(1);
                        }
                    }

                    singleThreaded = value;
                    ThreadSafety.SingleThreadThread = singleThreaded ? Thread.CurrentThread : null;
                });
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

        public void Start()
        {
            lock (threads)
            {
                if (singleThreaded)
                {
                    foreach (var t in threads)
                    {
                        t.OnThreadStart?.Invoke();
                        t.OnThreadStart = null;
                    }
                }
                else
                {
                    foreach (var t in threads)
                        t.Start();
                }
            }
        }

        private const int thread_join_timeout = 30000;

        public void Stop()
        {
            lock (threads)
            {
                threads.ForEach(t => t.Exit());
                threads.Where(t => t.Running).ForEach(t =>
                {
                    if (!t.Thread.Join(thread_join_timeout))
                        Logger.Log($"Thread {t.Name} failed to exit in allocated time ({thread_join_timeout}ms).", LoggingTarget.Runtime, LogLevel.Important);
                });
            }

            // as the input thread isn't actually handled by a thread, the above join does not necessarily mean it has been completed to an exiting state.
            while (!mainThread.Exited)
                mainThread.ProcessFrame();
        }
    }
}
