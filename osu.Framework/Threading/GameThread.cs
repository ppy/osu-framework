// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Threading;
using osu.Framework.Statistics;
using osu.Framework.Timing;
using System.Collections.Generic;

namespace osu.Framework.Threading
{
    public class GameThread
    {
        internal const double DEFAULT_ACTIVE_HZ = 1000;
        internal const double DEFAULT_INACTIVE_HZ = 60;

        internal PerformanceMonitor Monitor { get; }
        public ThrottledFrameClock Clock { get; }
        public Thread Thread { get; }
        public Scheduler Scheduler { get; }

        private readonly Action onNewFrame;

        private bool isActive = true;

        public bool IsActive
        {
            get { return isActive; }
            set
            {
                isActive = value;
                Clock.MaximumUpdateHz = isActive ? activeHz : inactiveHz;
            }
        }

        private double activeHz = DEFAULT_ACTIVE_HZ;

        public double ActiveHz
        {
            get { return activeHz; }

            set
            {
                activeHz = value;
                if (IsActive)
                    Clock.MaximumUpdateHz = activeHz;
            }
        }

        private double inactiveHz = DEFAULT_INACTIVE_HZ;

        public double InactiveHz
        {
            get { return inactiveHz; }

            set
            {
                inactiveHz = value;
                if (!IsActive)
                    Clock.MaximumUpdateHz = inactiveHz;
            }
        }

        public static string PrefixedThreadNameFor(string name) => $"{nameof(GameThread)}.{name}";

        public bool Running => Thread.IsAlive;

        private readonly ManualResetEvent initializedEvent = new ManualResetEvent(false);

        public Action OnThreadStart;

        internal virtual IEnumerable<StatisticsCounterType> StatisticsCounters => Array.Empty<StatisticsCounterType>();

        public readonly string Name;

        internal GameThread(Action onNewFrame, string name)
        {
            this.onNewFrame = onNewFrame;

            Thread = new Thread(runWork)
            {
                Name = PrefixedThreadNameFor(name),
                IsBackground = true,
            };

            Name = name;
            Clock = new ThrottledFrameClock();
            Monitor = new PerformanceMonitor(Clock, Thread, StatisticsCounters);
            Scheduler = new Scheduler(null, Clock);
        }

        public void WaitUntilInitialized()
        {
            initializedEvent.WaitOne();
        }

        private void runWork()
        {
            Scheduler.SetCurrentThread();

            OnThreadStart?.Invoke();

            initializedEvent.Set();

            while (!exitCompleted)
                ProcessFrame();
        }

        protected void ProcessFrame()
        {
            if (exitCompleted)
                return;

            if (exitRequested)
            {
                PerformExit();
                exitCompleted = true;
                return;
            }

            Monitor.NewFrame();

            using (Monitor.BeginCollecting(PerformanceCollectionType.Scheduler))
                Scheduler.Update();

            using (Monitor.BeginCollecting(PerformanceCollectionType.Work))
                onNewFrame?.Invoke();

            using (Monitor.BeginCollecting(PerformanceCollectionType.Sleep))
                Clock.ProcessFrame();
        }

        private volatile bool exitRequested;
        private volatile bool exitCompleted;

        public bool Exited => exitCompleted;

        public void Exit() => exitRequested = true;
        public void Start() => Thread?.Start();

        protected virtual void PerformExit()
        {
            Monitor?.Dispose();
            initializedEvent?.Dispose();
        }
    }
}
