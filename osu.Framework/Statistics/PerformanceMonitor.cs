// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Timing;
using osu.Framework.Threading;
using System.Diagnostics;

namespace osu.Framework.Statistics
{
    public class PerformanceMonitor
    {
        private StopwatchClock ourClock = new StopwatchClock(true);

        private Stack<PerformanceCollectionType> currentCollectionTypeStack = new Stack<PerformanceCollectionType>();

        private FrameStatistics currentFrame;

        private const int spike_time = 100;

        internal ConcurrentQueue<FrameStatistics> PendingFrames = new ConcurrentQueue<FrameStatistics>();
        internal ObjectStack<FrameStatistics> FramesHeap = new ObjectStack<FrameStatistics>(100);
        internal AtomicCounter[] Counters = new AtomicCounter[(int)StatisticsCounterType.AmountTypes];

        private double consumptionTime;

        internal IFrameBasedClock Clock;

        public double FrameAimTime => 1000.0 / (Clock as ThrottledFrameClock)?.MaximumUpdateHz ?? double.MaxValue;

        public PerformanceMonitor(IFrameBasedClock clock)
        {
            Clock = clock;
        }

        public void RegisterCounter(StatisticsCounterType type)
        {
            Counters[(int)type] = new AtomicCounter();
        }

        public AtomicCounter GetCounter(StatisticsCounterType counterType) => Counters[(int)counterType];

        /// <summary>
        /// Start collecting a type of passing time.
        /// </summary>
        public InvokeOnDisposal BeginCollecting(PerformanceCollectionType type)
        {
            if (currentCollectionTypeStack.Count > 0)
            {
                PerformanceCollectionType t = currentCollectionTypeStack.Peek();

                if (!currentFrame.CollectedTimes.ContainsKey(t)) currentFrame.CollectedTimes[t] = 0;
                currentFrame.CollectedTimes[t] += consumeStopwatchElapsedTime();
            }

            currentCollectionTypeStack.Push(type);

            return new InvokeOnDisposal(() => endCollecting(type));
        }

        /// <summary>
        /// End collecting a type of passing time (that was previously started).
        /// </summary>
        /// <param name="type"></param>
        private void endCollecting(PerformanceCollectionType type)
        {
            currentCollectionTypeStack.Pop();

            if (!currentFrame.CollectedTimes.ContainsKey(type)) currentFrame.CollectedTimes[type] = 0;
            currentFrame.CollectedTimes[type] += consumeStopwatchElapsedTime();
        }

        public int TargetFrameRate;

        private int[] lastAmountGarbageCollects = new int[3];

        public bool HandleGC = true;

        /// <summary>
        /// Resets all frame statistics. Run exactly once per frame.
        /// </summary>
        public void NewFrame()
        {
            if (currentFrame != null)
            {
                currentFrame.Postprocess();
                PendingFrames.Enqueue(currentFrame);
                if (PendingFrames.Count > 100)
                {
                    FrameStatistics oldFrame;
                    PendingFrames.TryDequeue(out oldFrame);
                }
            }

            currentFrame = FramesHeap.ReserveObject();
            currentFrame.Clear();

            if (HandleGC)
            {
                for (int i = 0; i < lastAmountGarbageCollects.Length; ++i)
                {
                    int amountCollections = GC.CollectionCount(i);
                    if (lastAmountGarbageCollects[i] != amountCollections)
                    {
                        lastAmountGarbageCollects[i] = amountCollections;
                        currentFrame.GarbageCollections.Add(i);
                    }
                }
            }

            for (int i = 0; i < (int)StatisticsCounterType.AmountTypes; ++i)
            {
                AtomicCounter counter = Counters[i];
                if (counter != null)
                    currentFrame.Counts[(StatisticsCounterType)i] = counter.Reset();
            }

            //check for dropped (stutter) frames
            if (Clock.ElapsedFrameTime > spike_time)
                newDroppedFrame();

            //reset frame totals
            currentCollectionTypeStack.Clear();
            //backgroundMonitorStackTrace = null;
            consumeStopwatchElapsedTime();
        }

        private double consumeStopwatchElapsedTime()
        {
            double last = consumptionTime;
            consumptionTime = ourClock.CurrentTime;
            return consumptionTime - last;
        }

        private void newDroppedFrame()
        {
        }

        internal double FramesPerSecond => Clock.FramesPerSecond;
        internal double AverageFrameTime => Clock.AverageFrameTime;
    }
}
