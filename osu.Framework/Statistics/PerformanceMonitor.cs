// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Allocation;
using osu.Framework.Timing;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace osu.Framework.Statistics
{
    internal class PerformanceMonitor : IDisposable
    {
        private readonly StopwatchClock ourClock = new StopwatchClock(true);

        private readonly Stack<PerformanceCollectionType> currentCollectionTypeStack = new Stack<PerformanceCollectionType>();

        private readonly InvokeOnDisposal[] endCollectionDelegates = new InvokeOnDisposal[FrameStatistics.NUM_PERFORMANCE_COLLECTION_TYPES];

        private readonly BackgroundStackTraceCollector traceCollector;

        private FrameStatistics currentFrame;

        private const int max_pending_frames = 100;

        internal readonly ConcurrentQueue<FrameStatistics> PendingFrames = new ConcurrentQueue<FrameStatistics>();
        internal readonly ObjectStack<FrameStatistics> FramesHeap = new ObjectStack<FrameStatistics>(max_pending_frames);
        private readonly bool[] activeCounters = new bool[FrameStatistics.NUM_STATISTICS_COUNTER_TYPES];

        internal bool[] ActiveCounters => (bool[])activeCounters.Clone();

        public bool EnablePerformanceProfiling
        {
            get => traceCollector.Enabled;
            set => traceCollector.Enabled = value;
        }

        private double consumptionTime;

        internal ThrottledFrameClock Clock;

        public double FrameAimTime => 1000.0 / Clock?.MaximumUpdateHz ?? double.MaxValue;

        internal PerformanceMonitor(ThrottledFrameClock clock, Thread thread, IEnumerable<StatisticsCounterType> counters)
        {
            Clock = clock;
            currentFrame = FramesHeap.ReserveObject();

            foreach (var c in counters)
                activeCounters[(int)c] = true;

            for (int i = 0; i < FrameStatistics.NUM_PERFORMANCE_COLLECTION_TYPES; i++)
            {
                var t = (PerformanceCollectionType)i;
                endCollectionDelegates[i] = new InvokeOnDisposal(() => endCollecting(t));
            }

            traceCollector = new BackgroundStackTraceCollector(thread, ourClock);
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
                PendingFrames.TryDequeue(out FrameStatistics oldFrame);
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
            traceCollector.NewFrame(Clock.ElapsedFrameTime, Math.Max(10, Math.Max(1000 / Clock.MaximumUpdateHz, AverageFrameTime) * 4));

            //reset frame totals
            currentCollectionTypeStack.Clear();
            consumeStopwatchElapsedTime();
        }

        private double consumeStopwatchElapsedTime()
        {
            double last = consumptionTime;

            consumptionTime = traceCollector.LastConsumptionTime = ourClock.CurrentTime;

            return consumptionTime - last;
        }

        internal double FramesPerSecond => Clock.FramesPerSecond;
        internal double AverageFrameTime => Clock.AverageFrameTime;

        #region IDisposable Support

        private bool isDisposed;

        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                isDisposed = true;
                traceCollector.Dispose();
            }
        }

        ~PerformanceMonitor()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
