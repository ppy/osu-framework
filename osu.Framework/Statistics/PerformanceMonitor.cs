﻿// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Allocation;
using osu.Framework.Timing;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace osu.Framework.Statistics
{
    internal class PerformanceMonitor
    {
        private readonly StopwatchClock ourClock = new StopwatchClock(true);

        private readonly Stack<PerformanceCollectionType> currentCollectionTypeStack = new Stack<PerformanceCollectionType>();

        private readonly InvokeOnDisposal[] endCollectionDelegates = new InvokeOnDisposal[FrameStatistics.NumPerformanceCollectionTypes];

        private FrameStatistics currentFrame;

        private const int spike_time = 100;

        private const int max_pending_frames = 100;

        internal readonly ConcurrentQueue<FrameStatistics> PendingFrames = new ConcurrentQueue<FrameStatistics>();
        internal readonly ObjectStack<FrameStatistics> FramesHeap = new ObjectStack<FrameStatistics>(max_pending_frames);
        private readonly bool[] activeCounters = new bool[FrameStatistics.NumStatisticsCounterTypes];

        internal bool[] ActiveCounters => (bool[])activeCounters.Clone();

        private double consumptionTime;

        internal IFrameBasedClock Clock;

        public double FrameAimTime => 1000.0 / (Clock as ThrottledFrameClock)?.MaximumUpdateHz ?? double.MaxValue;

        internal PerformanceMonitor(IFrameBasedClock clock, IEnumerable<StatisticsCounterType> counters)
        {
            Clock = clock;
            currentFrame = FramesHeap.ReserveObject();

            foreach (var c in counters)
                activeCounters[(int)c] = true;

            for (int i = 0; i < FrameStatistics.NumPerformanceCollectionTypes; i++)
            {
                var t = (PerformanceCollectionType)i;
                endCollectionDelegates[i] = new InvokeOnDisposal(() => endCollecting(t));
            }
        }

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

            return endCollectionDelegates[(int)type];
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

        private readonly int[] lastAmountGarbageCollects = new int[3];

        public bool HandleGC = true;

        /// <summary>
        /// Resets all frame statistics. Run exactly once per frame.
        /// </summary>
        public void NewFrame()
        {
            // Reset the counters we keep track of
            for (int i = 0; i < activeCounters.Length; ++i)
                if (activeCounters[i])
                {
                    currentFrame.Counts[(StatisticsCounterType)i] = FrameStatistics.COUNTERS[i];
                    FrameStatistics.COUNTERS[i] = 0;
                }

            PendingFrames.Enqueue(currentFrame);
            if (PendingFrames.Count >= max_pending_frames)
            {
                FrameStatistics oldFrame;
                PendingFrames.TryDequeue(out oldFrame);
                FramesHeap.FreeObject(oldFrame);
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
