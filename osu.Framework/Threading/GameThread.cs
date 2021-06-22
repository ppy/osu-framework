// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Threading;
using osu.Framework.Statistics;
using osu.Framework.Timing;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using JetBrains.Annotations;
using osu.Framework.Bindables;
using osu.Framework.Development;
using osu.Framework.Platform;

namespace osu.Framework.Threading
{
    public class GameThread
    {
        internal const double DEFAULT_ACTIVE_HZ = 1000;
        internal const double DEFAULT_INACTIVE_HZ = 60;

        internal PerformanceMonitor Monitor { get; }
        public ThrottledFrameClock Clock { get; }

        /// <summary>
        /// The dedicated OS thread for this <see cref="GameThread"/>.
        /// A value of <see langword="null"/> does not necessarily mean that this thread is not running;
        /// in <see cref="ExecutionMode.SingleThread"/> execution mode <see cref="ThreadRunner"/> drives its <see cref="GameThread"/>s
        /// manually and sequentially on the main OS thread of the game process.
        /// </summary>
        [CanBeNull]
        public Thread Thread { get; private set; }

        public Scheduler Scheduler { get; }

        /// <summary>
        /// Attach a handler to delegate responsibility for per-frame exceptions.
        /// While attached, all exceptions will be caught and forwarded. Thread execution will continue indefinitely.
        /// </summary>
        public EventHandler<UnhandledExceptionEventArgs> UnhandledException;

        protected Action OnNewFrame;

        /// <summary>
        /// Whether the game is active (in the foreground).
        /// </summary>
        public readonly IBindable<bool> IsActive = new Bindable<bool>(true);

        private double activeHz = DEFAULT_ACTIVE_HZ;

        public double ActiveHz
        {
            get => activeHz;
            set
            {
                activeHz = value;
                updateMaximumHz();
            }
        }

        private double inactiveHz = DEFAULT_INACTIVE_HZ;

        public double InactiveHz
        {
            get => inactiveHz;
            set
            {
                inactiveHz = value;
                updateMaximumHz();
            }
        }

        public static string PrefixedThreadNameFor(string name) => $"{nameof(GameThread)}.{name}";

        public bool Running { get; private set; }

        public virtual bool IsCurrent => true;

        private readonly ManualResetEvent initializedEvent = new ManualResetEvent(false);

        private readonly object startStopLock = new object();

        /// <summary>
        /// Whether a pause has been requested.
        /// </summary>
        private bool pauseRequested;

        internal void Initialize(bool withThrottling)
        {
            MakeCurrent();

            OnInitialize();

            Clock.Throttling = withThrottling;

            Monitor.MakeCurrent();

            updateCulture();

            initializedEvent.Set();
        }

        protected virtual void OnInitialize() { }

        internal virtual IEnumerable<StatisticsCounterType> StatisticsCounters => Array.Empty<StatisticsCounterType>();

        public readonly string Name;

        internal GameThread(Action onNewFrame = null, string name = "unknown", bool monitorPerformance = true)
        {
            OnNewFrame = onNewFrame;

            Name = name;
            Clock = new ThrottledFrameClock();
            if (monitorPerformance)
                Monitor = new PerformanceMonitor(this, StatisticsCounters);
            Scheduler = new GameThreadScheduler(this);

            IsActive.BindValueChanged(_ => updateMaximumHz(), true);
        }

        private void createThread()
        {
            Debug.Assert(Thread == null);
            Debug.Assert(!Running);

            Thread = new Thread(runWork)
            {
                Name = PrefixedThreadNameFor(Name),
                IsBackground = true,
            };

            updateCulture();
        }

        public void WaitUntilInitialized()
        {
            initializedEvent.WaitOne();
        }

        private void updateMaximumHz() => Scheduler.Add(() => Clock.MaximumUpdateHz = IsActive.Value ? activeHz : inactiveHz);

        private void runWork()
        {
            Running = true;

            try
            {
                Initialize(true);

                while (!exitCompleted && !pauseRequested)
                {
                    ProcessFrame();
                }
            }
            finally
            {
                OnPause();
                Cleanup();
            }
        }

        /// <summary>
        /// Run when thread transitions into an active/processing state.
        /// </summary>
        internal virtual void MakeCurrent()
        {
            ThreadSafety.ResetAllForCurrentThread();
        }

        internal void ProcessFrame()
        {
            try
            {
                if (exitCompleted)
                    return;

                MakeCurrent();

                if (exitRequested)
                {
                    PerformExit();
                    exitCompleted = true;
                    return;
                }

                Monitor?.NewFrame();

                using (Monitor?.BeginCollecting(PerformanceCollectionType.Scheduler))
                    Scheduler.Update();

                using (Monitor?.BeginCollecting(PerformanceCollectionType.Work))
                    OnNewFrame?.Invoke();

                using (Monitor?.BeginCollecting(PerformanceCollectionType.Sleep))
                    Clock.ProcessFrame();

                Monitor?.EndFrame();
            }
            catch (Exception e)
            {
                if (UnhandledException != null && !ThreadSafety.IsInputThread)
                    // the handler schedules back to the input thread, so don't run it if we are already on the input thread
                    UnhandledException.Invoke(this, new UnhandledExceptionEventArgs(e, false));
                else
                    throw;
            }
        }

        private volatile bool exitRequested;
        private volatile bool exitCompleted;

        public bool Exited => exitCompleted;

        private CultureInfo culture;

        public CultureInfo CurrentCulture
        {
            get => culture;
            set
            {
                culture = value;

                updateCulture();
            }
        }

        private void updateCulture()
        {
            if (Thread == null || culture == null) return;

            Thread.CurrentCulture = culture;
            Thread.CurrentUICulture = culture;
        }

        public void Pause()
        {
            lock (startStopLock)
            {
                if (Thread == null)
                {
                    // run the OnPause() logic as the GameThread may have been run manually by ThreadRunner via ProcessFrame() calls.
                    OnPause();
                    return;
                }

                pauseRequested = true;
            }

            while (Running)
                Thread.Sleep(1);
        }

        /// <summary>
        /// Called when a <see cref="Pause"/> or <see cref="Exit"/> is requested on this <see cref="GameThread"/>.
        /// Use this method to release exclusive resources that the thread could have been holding in its current execution mode,
        /// like GL contexts or similar.
        /// </summary>
        protected virtual void OnPause()
        {
        }

        protected void Cleanup()
        {
            lock (startStopLock)
            {
                Thread = null;
                Running = false;
            }
        }

        public void Exit() => exitRequested = true;

        public virtual void Start()
        {
            lock (startStopLock)
            {
                pauseRequested = false;

                Debug.Assert(Thread == null);
                createThread();
                Debug.Assert(Thread != null);

                Thread.Start();

                while (!Running)
                    Thread.Sleep(1);
            }
        }

        protected virtual void PerformExit()
        {
            Monitor?.Dispose();
            initializedEvent?.Dispose();
        }

        public class GameThreadScheduler : Scheduler
        {
            public GameThreadScheduler(GameThread thread)
                : base(() => thread.IsCurrent, thread.Clock)
            {
            }
        }
    }
}
