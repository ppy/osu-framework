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

        /// <summary>
        /// The current state of this thread.
        /// </summary>
        public IBindable<GameThreadState> State => state;

        private readonly Bindable<GameThreadState> state = new Bindable<GameThreadState>();

        internal PerformanceMonitor Monitor { get; }

        public ThrottledFrameClock Clock { get; }

        /// <summary>
        /// The current dedicated OS thread for this <see cref="GameThread"/>.
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

        public bool Running => state.Value == GameThreadState.Running;

        public bool Exited => state.Value == GameThreadState.Exited;

        /// <summary>
        /// Whether currently executing on this thread (from the point of invocation).
        /// </summary>
        public virtual bool IsCurrent => true;

        private readonly ManualResetEvent initializedEvent = new ManualResetEvent(false);

        private readonly object startStopLock = new object();

        /// <summary>
        /// Whether a pause has been requested.
        /// </summary>
        private volatile bool pauseRequested;

        /// <summary>
        /// Whether an exit has been requested.
        /// </summary>
        private volatile bool exitRequested;

        /// <summary>
        /// Prepare this thread for performing work.
        /// Must be called when entering a running state.
        /// </summary>
        /// <param name="withThrottling">Whether this thread's clock should be throttling via thread sleeps.</param>
        internal void Initialize(bool withThrottling)
        {
            lock (startStopLock)
            {
                Debug.Assert(state.Value != GameThreadState.Running);
                Debug.Assert(state.Value != GameThreadState.Exited);

                MakeCurrent();

                state.Value = GameThreadState.Running;
            }

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

        public void WaitUntilInitialized() => initializedEvent.WaitOne();

        private void updateMaximumHz() => Scheduler.Add(() => Clock.MaximumUpdateHz = IsActive.Value ? activeHz : inactiveHz);

        private void runWork()
        {
            Initialize(true);

            while (true)
            {
                bool shouldContinue = ProcessFrame();

                if (!shouldContinue)
                {
                    Thread = null;
                    break;
                }
            }
        }

        /// <summary>
        /// Run when thread transitions into an active/processing state.
        /// </summary>
        internal virtual void MakeCurrent()
        {
            ThreadSafety.ResetAllForCurrentThread();
        }

        /// <summary>
        /// Process a single frame of this thread's work.
        /// </summary>
        /// <returns>Whether execution is still valid.</returns>
        internal bool ProcessFrame()
        {
            if (state.Value != GameThreadState.Running)
                // host could be in a suspended state. the input thread will still make calls to ProcessFrame so we can't throw.
                return false;

            try
            {
                MakeCurrent();

                if (exitRequested || pauseRequested)
                {
                    lock (startStopLock)
                    {
                        if (state.Value != GameThreadState.Running)
                            throw new InvalidOperationException($"Attempted to process frame when state is {state.Value}");

                        if (exitRequested)
                        {
                            state.Value = GameThreadState.Exited;
                            exitRequested = false;
                            PerformExit();
                        }
                        else
                        {
                            state.Value = GameThreadState.Paused;
                            pauseRequested = false;
                        }
                    }

                    OnSuspended();
                    return false;
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

            return true;
        }

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
                if (state.Value != GameThreadState.Running)
                    return;

                // actual pause will be done in ProcessFrame.
                pauseRequested = true;
            }

            if (Thread == null)
            {
                // if the thread is null at this point, presume the Pause call was made by the ThreadRunner in SingleThread execution.
                // run frames until the pause has completed.
                while (Running)
                    ProcessFrame();
            }
            else
            {
                while (Running)
                    Thread.Sleep(1);
            }
        }

        /// <summary>
        /// Called when a <see cref="Pause"/> or <see cref="Exit"/> is requested on this <see cref="GameThread"/>.
        /// Use this method to release exclusive resources that the thread could have been holding in its current execution mode,
        /// like GL contexts or similar.
        /// </summary>
        protected virtual void OnSuspended()
        {
        }

        public void Exit()
        {
            lock (startStopLock)
            {
                if (state.Value == GameThreadState.Paused)
                    // technically we could support this, but we don't use this yet and it will add more complexity.
                    throw new InvalidOperationException("Cannot exit when thread is paused");

                // actual exit will be done in ProcessFrame.
                exitRequested = true;
            }
        }

        /// <summary>
        /// Start this thread.
        /// </summary>
        public void Start()
        {
            lock (startStopLock)
            {
                Debug.Assert(state.Value != GameThreadState.Exited);
                Debug.Assert(state.Value != GameThreadState.Running);

                PrepareForWork();
            }
        }

        /// <summary>
        /// Prepares this game thread for work. Should block until <see cref="Initialize"/> has been run.
        /// </summary>
        protected virtual void PrepareForWork()
        {
            Debug.Assert(Thread == null);
            createThread();
            Debug.Assert(Thread != null);

            Thread.Start();

            while (!Running)
                Thread.Sleep(1);
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
