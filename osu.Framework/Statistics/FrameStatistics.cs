// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.Performance;
using System.Collections.Generic;
using System.Diagnostics;

namespace osu.Framework.Statistics
{
    public class FrameStatistics
    {
        internal Dictionary<PerformanceCollectionType, double> CollectedTimes = new Dictionary<PerformanceCollectionType, double>();
        internal Dictionary<StatisticsCounterType, long> Counts = new Dictionary<StatisticsCounterType, long>();
        internal List<int> GarbageCollections = new List<int>();

        internal void Clear()
        {
            CollectedTimes.Clear();
            GarbageCollections.Clear();
            Counts.Clear();
        }

        private static PerformanceMonitor getMonitor(StatisticsCounterType type, PerformanceOverlay target)
        {
            switch (type)
            {
                case StatisticsCounterType.Invalidations:
                case StatisticsCounterType.Refreshes:
                case StatisticsCounterType.DrawNodeCtor:
                case StatisticsCounterType.DrawNodeAppl:
                case StatisticsCounterType.ScheduleInvk:
                    return target.Threads[2].Monitor;

                case StatisticsCounterType.VBufOverflow:
                case StatisticsCounterType.TextureBinds:
                case StatisticsCounterType.DrawCalls:
                case StatisticsCounterType.VerticesDraw:
                case StatisticsCounterType.VerticesUpl:
                case StatisticsCounterType.KiloPixels:
                    return target.Threads[3].Monitor;

                case StatisticsCounterType.TasksRun:
                case StatisticsCounterType.Tracks:
                case StatisticsCounterType.Samples:
                case StatisticsCounterType.SChannels:
                case StatisticsCounterType.Components:
                    return target.Threads[1].Monitor;

                default:
                    Trace.Assert(false, "Requested counter which is not assigned to any performance monitor.");
                    break;
            }

            return null;
        }

        internal void Postprocess()
        {
            if (Counts.ContainsKey(StatisticsCounterType.KiloPixels))
                Counts[StatisticsCounterType.KiloPixels] /= 1000;
        }

        /// <summary>
        /// Registers statistics counters to the performance overlay first passed into this function
        /// </summary>
        /// <param name="target">The overlay that wants to register for statistics counters.</param>
        internal static void RegisterCounters(PerformanceOverlay target)
        {
            if (FrameStatistics.target != null)
                return;

            for (StatisticsCounterType i = 0; i < StatisticsCounterType.AmountTypes; ++i)
                getMonitor(i, target).RegisterCounter(i);

            FrameStatistics.target = target;
        }

        internal static void Increment(StatisticsCounterType type, long amount = 1)
        {
            if (target == null || amount == 0)
                return;

            getMonitor(type, target)?.GetCounter(type)?.Add(amount);
        }

        private static PerformanceOverlay target;
    }

    public enum PerformanceCollectionType
    {
        Work = 0,
        SwapBuffer,
        WndProc,
        Debug,
        Sleep,
        Scheduler,
        IPC,
        GLReset,
        Empty,
    }

    public enum StatisticsCounterType
    {
        Invalidations = 0,
        Refreshes,
        DrawNodeCtor,
        DrawNodeAppl,
        ScheduleInvk,

        VBufOverflow,
        TextureBinds,
        DrawCalls,
        VerticesDraw,
        VerticesUpl,
        KiloPixels,

        TasksRun,
        Tracks,
        Samples,
        SChannels,
        Components,

        AmountTypes,
    }
}
