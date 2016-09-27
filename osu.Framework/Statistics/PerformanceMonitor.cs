﻿// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Timing;

//using System.Diagnostics.PerformanceData;

namespace osu.Framework.Statistics
{
    public class PerformanceMonitor
    {
        private StopwatchClock ourClock = new StopwatchClock(true);

        private Stack<PerformanceCollectionType> CurrentCollectionTypeStack = new Stack<PerformanceCollectionType>();

        private FrameStatistics currentFrame;

        internal ConcurrentQueue<FrameStatistics> PendingFrames = new ConcurrentQueue<FrameStatistics>();
        internal ObjectStack<FrameStatistics> FramesHeap = new ObjectStack<FrameStatistics>(100);

        private double consumptionTime;

        internal double FramesPerSecond;
        internal double AverageFrameTime;

        double timeUntilNextCalculation;
        double timeSinceLastCalculation;
        int framesSinceLastCalculation;

        const int fps_calculation_interval = 250;

        internal IFrameBasedClock Clock;

        public double FrameAimTime => 1000 / (Clock as ThrottledFrameClock)?.MaximumUpdateHz ?? double.MaxValue;

        public PerformanceMonitor(IFrameBasedClock clock)
        {
            Clock = clock;
        }

        //internal void ReportCount(CounterType type)
        //{
        //    //todo: thread safety? Interlocked.Increment?
        //    if (!currentFrame.CollectedCounters.ContainsKey(type))
        //        currentFrame.CollectedCounters[type] = 1;
        //    else
        //        currentFrame.CollectedCounters[type]++;
        //}

        /// <summary>
        /// Start collecting a type of passing time.
        /// </summary>
        internal InvokeOnDisposal BeginCollecting(PerformanceCollectionType type)
        {
            if (CurrentCollectionTypeStack.Count > 0)
            {
                PerformanceCollectionType t = CurrentCollectionTypeStack.Peek();

                if (!currentFrame.CollectedTimes.ContainsKey(t)) currentFrame.CollectedTimes[t] = 0;
                currentFrame.CollectedTimes[t] += consumeStopwatchElapsedTime();
            }

            CurrentCollectionTypeStack.Push(type);

            return new InvokeOnDisposal(() => EndCollecting(type));
        }

        private double consumeStopwatchElapsedTime()
        {
            double last = consumptionTime;
            consumptionTime = ourClock.CurrentTime;
            return consumptionTime - last;
        }

        /// <summary>
        /// End collecting a type of passing time (that was previously started).
        /// </summary>
        /// <param name="type"></param>
        private void EndCollecting(PerformanceCollectionType type)
        {
            CurrentCollectionTypeStack.Pop();

            if (!currentFrame.CollectedTimes.ContainsKey(type)) currentFrame.CollectedTimes[type] = 0;
            currentFrame.CollectedTimes[type] += consumeStopwatchElapsedTime();
        }

        public int TargetFrameRate;

        private int[] lastAmountGarbageCollects = new int[3];

        public bool HandleGC = true;

        /// <summary>
        /// Resets all frame statistics. Run exactly once per frame.
        /// </summary>
        internal void NewFrame()
        {
            if (currentFrame != null)
            {
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

            //update framerate
            double decay = Math.Pow(0.05, Clock.ElapsedFrameTime);

            framesSinceLastCalculation++;
            timeUntilNextCalculation -= Clock.ElapsedFrameTime;
            timeSinceLastCalculation += Clock.ElapsedFrameTime;

            if (timeUntilNextCalculation <= 0)
            {
                timeUntilNextCalculation += fps_calculation_interval;

                FramesPerSecond = framesSinceLastCalculation == 0 ? 0 : (int)Math.Ceiling(Math.Min(framesSinceLastCalculation * 1000f / timeSinceLastCalculation, TargetFrameRate));
                timeSinceLastCalculation = framesSinceLastCalculation = 0;
            }

            //check for dropped (stutter) frames
            //if (framedClock.ElapsedFrameTime > spikeTime)
            //NewDroppedFrame();

            AverageFrameTime = decay * AverageFrameTime + (1 - decay) * Clock.ElapsedFrameTime;

            //reset frame totals
            CurrentCollectionTypeStack.Clear();
            //backgroundMonitorStackTrace = null;
            consumeStopwatchElapsedTime();
        }
    }
}
