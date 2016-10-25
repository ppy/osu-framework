// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Threading;
using osu.Framework.Statistics;
using osu.Framework.Timing;

namespace osu.Framework.Threading
{
    public class GameThread
    {
        public PerformanceMonitor Monitor { get; }
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

        private double activeHz = 1000;
        public double ActiveHz
        {
            get
            {
                return activeHz;
            }

            set
            {
                activeHz = value;
                if (IsActive)
                    Clock.MaximumUpdateHz = activeHz;
            }
        }

        private double inactiveHz = 40;
        public double InactiveHz
        {
            get
            {
                return inactiveHz;
            }

            set
            {
                inactiveHz = value;
                if (!IsActive)
                    Clock.MaximumUpdateHz = inactiveHz;
            }
        }

        public bool Running => Thread.IsAlive;

        public void Exit() => exitRequested = true;

        private bool exitRequested;

        public Action OnThreadStart;

        public GameThread(Action onNewFrame, string threadName)
        {
            this.onNewFrame = onNewFrame;
            Thread = new Thread(runWork)
            {
                Name = threadName,
                IsBackground = true,
            };

            Clock = new ThrottledFrameClock();
            Monitor = new PerformanceMonitor(Clock);
            Scheduler = new Scheduler(null);
        }

        private void runWork()
        {
            Scheduler.SetCurrentThread();

            OnThreadStart?.Invoke();

            while (!exitRequested)
                ProcessFrame();
        }

        protected void ProcessFrame()
        {
            Monitor.NewFrame();

            using (Monitor.BeginCollecting(PerformanceCollectionType.Scheduler))
                Scheduler.Update();

            onNewFrame?.Invoke();

            using (Monitor.BeginCollecting(PerformanceCollectionType.Sleep))
                Clock.ProcessFrame();
        }

        public void Start()
        {
            Thread?.Start();
        }
    }
}