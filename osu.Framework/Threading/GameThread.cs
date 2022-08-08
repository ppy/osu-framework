// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using osu.Framework.Bindables;
using osu.Framework.Development;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Platform;
using osu.Framework.Statistics;
using osu.Framework.Timing;

namespace osu.Framework.Threading
{
    /// <summary>
    /// A conceptual thread used for running game work. May or may not be backed by a native thread.
    /// </summary>
    public class GameThread
    {
        internal const int DEFAULT_ACTIVE_HZ = 1000;
        internal const int DEFAULT_INACTIVE_HZ = 60;

        /// <summary>
        /// The name of this thread.
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// Whether the game is active (in the foreground).
        /// </summary>
        public readonly IBindable<bool> IsActive = new Bindable<bool>(true);

        /// <summary>
        /// The current state of this thread.
        /// </summary>
        public IBindable<GameThreadState> State => state;

        private readonly Bindable<GameThreadState> state = new Bindable<GameThreadState>();

        /// <summary>
        /// Whether this thread is currently running.
        /// </summary>
        public bool Running => state.Value == GameThreadState.Running;

        /// <summary>
        /// Whether this thread is exited.
        /// </summary>
        public bool Exited => state.Value == GameThreadState.Exited;

        /// <summary>
        /// Whether currently executing on this thread (from the point of invocation).
        /// </summary>
        public virtual bool IsCurrent => true;

        /// <summary>
        /// The thread's clock. Responsible for timekeeping and throttling.
        /// </summary>
        public ThrottledFrameClock Clock { get; }

        /// <summary>
        /// The current dedicated OS thread for this <see cref="GameThread"/>.
        /// A value of <see langword="null"/> does not necessarily mean that this thread is not running;
        /// in <see cref="ExecutionMode.SingleThread"/> execution mode <see cref="ThreadRunner"/> drives its <see cref="GameThread"/>s
        /// manually and sequentially on the main OS thread of the game process.
        /// </summary>
        public Thread? Thread { get; private set; }

        /// <summary>
        /// The thread's scheduler.
        /// </summary>
        public Scheduler Scheduler { get; }

        /// <summary>
        /// Attach a handler to delegate responsibility for per-frame exceptions.
        /// While attached, all exceptions will be caught and forwarded. Thread execution will continue indefinitely.
        /// </summary>
        public EventHandler<UnhandledExceptionEventArgs>? UnhandledException;

        /// <summary>
        /// A synchronisation context which posts to this thread.
        /// </summary>
        public SynchronizationContext SynchronizationContext => synchronizationContext;

        /// <summary>
        /// The culture of this thread.
        /// </summary>
        public CultureInfo? CurrentCulture
        {
            get => culture;
            set
            {
                culture = value;

                updateCulture();
            }
        }

        private CultureInfo? culture;

        /// <summary>
        /// The target number of updates per second when the game window is active.
        /// </summary>
        /// <remarks>
        /// A value of 0 is treated the same as "unlimited" or <see cref="double.MaxValue"/>.
        /// </remarks>
        public double ActiveHz
        {
            get => activeHz;
            set
            {
                activeHz = value;
                updateMaximumHz();
            }
        }

        private double activeHz = DEFAULT_ACTIVE_HZ;

        /// <summary>
        /// The target number of updates per second when the game window is inactive.
        /// </summary>
        /// <remarks>
        /// A value of 0 is treated the same as "unlimited" or <see cref="double.MaxValue"/>.
        /// </remarks>
        public double InactiveHz
        {
            get => inactiveHz;
            set
            {
                inactiveHz = value;
                updateMaximumHz();
            }
        }

        private double inactiveHz = DEFAULT_INACTIVE_HZ;

        private readonly GameThreadSynchronizationContext synchronizationContext;

        internal PerformanceMonitor? Monitor { get; }

        internal virtual IEnumerable<StatisticsCounterType> StatisticsCounters => Array.Empty<StatisticsCounterType>();

        /// <summary>
        /// The main work which is fired on each frame.
        /// </summary>
        protected event Action? OnNewFrame;

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

        internal GameThread(Action? onNewFrame = null, string name = "unknown", bool monitorPerformance = true)
        {
            OnNewFrame = onNewFrame;

            Name = name;
            Clock = new ThrottledFrameClock();
            if (monitorPerformance)
                Monitor = new PerformanceMonitor(this, StatisticsCounters);

            Scheduler = new GameThreadScheduler(this);
            synchronizationContext = new GameThreadSynchronizationContext(this);

            IsActive.BindValueChanged(_ => updateMaximumHz(), true);
        }

        /// <summary>
        /// Block until this thread has entered an initialised state.
        /// </summary>
        public void WaitUntilInitialized()
        {
            initializedEvent.WaitOne();
        }

        /// <summary>
        /// Returns a string representation that is prefixed with this thread's identifier.
        /// </summary>
        /// <param name="name">The content to prefix.</param>
        /// <returns>A prefixed string.</returns>
        public static string PrefixedThreadNameFor(string name) => $"{nameof(GameThread)}.{name}";

        /// <summary>
        /// Start this thread.
        /// </summary>
        /// <remarks>
        /// This method blocks until in a running state.
        /// </remarks>
        public void Start()
        {
            lock (startStopLock)
            {
                switch (state.Value)
                {
                    case GameThreadState.Paused:
                    case GameThreadState.NotStarted:
                        break;

                    default:
                        throw new InvalidOperationException($"Cannot start when thread is {state.Value}.");
                }

                state.Value = GameThreadState.Starting;
                PrepareForWork();
            }

            WaitForState(GameThreadState.Running);
            Debug.Assert(state.Value == GameThreadState.Running);
        }

        /// <summary>
        /// Request that this thread is exited.
        /// </summary>
        /// <remarks>
        /// This does not block and will only queue an exit request, which is processed in the main frame loop.
        /// </remarks>
        /// <exception cref="InvalidOperationException">Thrown when attempting to exit from an invalid state.</exception>
        public void Exit()
        {
            lock (startStopLock)
            {
                switch (state.Value)
                {
                    // technically we could support this, but we don't use this yet and it will add more complexity.
                    case GameThreadState.Paused:
                    case GameThreadState.NotStarted:
                    case GameThreadState.Starting:
                        throw new InvalidOperationException($"Cannot exit when thread is {state.Value}.");

                    case GameThreadState.Exited:
                        return;

                    default:
                        // actual exit will be done in ProcessFrame.
                        exitRequested = true;
                        break;
                }
            }
        }

        /// <summary>
        /// Prepare this thread for performing work. Must be called when entering a running state.
        /// </summary>
        /// <param name="withThrottling">Whether this thread's clock should be throttling via thread sleeps.</param>
        internal void Initialize(bool withThrottling)
        {
            lock (startStopLock)
            {
                Debug.Assert(state.Value != GameThreadState.Running);
                Debug.Assert(state.Value != GameThreadState.Exited);

                MakeCurrent();

                OnInitialize();

                Clock.Throttling = withThrottling;

                Monitor?.MakeCurrent();

                updateCulture();

                initializedEvent.Set();
                state.Value = GameThreadState.Running;
            }
        }

        /// <summary>
        /// Run when thread transitions into an active/processing state, at the beginning of each frame.
        /// </summary>
        internal virtual void MakeCurrent()
        {
            ThreadSafety.ResetAllForCurrentThread();

            SynchronizationContext.SetSynchronizationContext(synchronizationContext);
        }

        /// <summary>
        /// Runs a single frame, updating the execution state if required.
        /// </summary>
        internal void RunSingleFrame()
        {
            var newState = processFrame();

            if (newState.HasValue)
                setExitState(newState.Value);
        }

        /// <summary>
        /// Pause this thread. Must be run from <see cref="ThreadRunner"/> in a safe manner.
        /// </summary>
        /// <remarks>
        /// This method blocks until in a paused state.
        /// </remarks>
        internal void Pause()
        {
            lock (startStopLock)
            {
                if (state.Value != GameThreadState.Running)
                    return;

                // actual pause will be done in ProcessFrame.
                pauseRequested = true;
            }

            WaitForState(GameThreadState.Paused);
        }

        /// <summary>
        /// Spin indefinitely until this thread enters a required state.
        /// For cases where no native thread is present, this will run <see cref="processFrame"/> until the required state is reached.
        /// </summary>
        /// <param name="targetState">The state to wait for.</param>
        internal void WaitForState(GameThreadState targetState)
        {
            if (state.Value == targetState)
                return;

            if (Thread == null)
            {
                GameThreadState? newState = null;

                // if the thread is null at this point, we need to assume that this WaitForState call is running on the same native thread as this GameThread has/will be running.
                // run frames until the required state is reached.
                while (newState != targetState)
                    newState = processFrame();

                // note that the only state transition here can be an exiting one. entering a running state can only occur in Initialize().
                setExitState(newState.Value);
            }
            else
            {
                while (state.Value != targetState)
                    Thread.Sleep(1);
            }

            Debug.Assert(state.Value == targetState);
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
        }

        /// <summary>
        /// Called whenever the thread is initialised. Should prepare the thread for performing work.
        /// </summary>
        protected virtual void OnInitialize()
        {
        }

        /// <summary>
        /// Called when a <see cref="Pause"/> or <see cref="Exit"/> is requested on this <see cref="GameThread"/>.
        /// Use this method to release exclusive resources that the thread could have been holding in its current execution mode,
        /// like GL contexts or similar.
        /// </summary>
        protected virtual void OnSuspended()
        {
        }

        /// <summary>
        /// Called when the thread is exited. Should clean up any thread-specific resources.
        /// </summary>
        protected virtual void OnExit()
        {
        }

        private void updateMaximumHz()
        {
            Scheduler.Add(() => Clock.MaximumUpdateHz = IsActive.Value ? activeHz : inactiveHz);
        }

        /// <summary>
        /// Create the native backing thread to run work.
        /// </summary>
        /// <remarks>
        /// This does not start the thread, but guarantees <see cref="Thread"/> is non-null.
        /// </remarks>
        private void createThread()
        {
            Debug.Assert(Thread == null);
            Debug.Assert(!Running);

            Thread = new Thread(runWork)
            {
                Name = PrefixedThreadNameFor(Name),
                IsBackground = true,
            };

            void runWork()
            {
                Initialize(true);

                while (Running)
                    RunSingleFrame();
            }
        }

        /// <summary>
        /// Process a single frame of this thread's work.
        /// </summary>
        /// <returns>A potential execution state change.</returns>
        private GameThreadState? processFrame()
        {
            if (state.Value != GameThreadState.Running)
                // host could be in a suspended state. the input thread will still make calls to ProcessFrame so we can't throw.
                return null;

            MakeCurrent();

            if (exitRequested)
            {
                exitRequested = false;
                return GameThreadState.Exited;
            }

            if (pauseRequested)
            {
                pauseRequested = false;
                return GameThreadState.Paused;
            }

            try
            {
                Monitor?.NewFrame();

                using (Monitor?.BeginCollecting(PerformanceCollectionType.Scheduler))
                {
                    Scheduler.Update();
                    synchronizationContext.RunWork();
                }

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

            return null;
        }

        private void updateCulture()
        {
            if (Thread == null || culture == null) return;

            Thread.CurrentCulture = culture;
            Thread.CurrentUICulture = culture;
        }

        private void setExitState(GameThreadState exitState)
        {
            lock (startStopLock)
            {
                Debug.Assert(state.Value == GameThreadState.Running);
                Debug.Assert(exitState == GameThreadState.Exited || exitState == GameThreadState.Paused);

                Thread = null;
                OnSuspended();

                switch (exitState)
                {
                    case GameThreadState.Exited:
                        Monitor?.Dispose();

                        if (initializedEvent.IsNotNull())
                            initializedEvent.Dispose();

                        synchronizationContext.DisassociateGameThread();

                        OnExit();
                        break;
                }

                state.Value = exitState;
            }
        }
    }
}
