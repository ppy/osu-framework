// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Platform;
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

        private static PerformanceMonitor getMonitor(StatisticsCounterType type)
        {
            switch (type)
            {
                case StatisticsCounterType.Invalidations:
                case StatisticsCounterType.Refreshes:
                case StatisticsCounterType.DrawNodeCtor:
                case StatisticsCounterType.DrawNodeAppl:
                case StatisticsCounterType.ScheduleInvk:
                    return target.UpdateMonitor;

                case StatisticsCounterType.VBufOverflow:
                case StatisticsCounterType.TextureBinds:
                case StatisticsCounterType.DrawCalls:
                case StatisticsCounterType.VerticesDraw:
                case StatisticsCounterType.VerticesUpl:
                case StatisticsCounterType.KiloPixels:
                    return target.DrawMonitor;

                default:
                    Debug.Assert(false, "Requested counter which is not assigned to any performance monitor.");
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
        /// Registers statistics counters to the PerformanceMonitors of the _first_ host
        /// to call this function.
        /// </summary>
        /// <param name="target">The host that wants to register for statistics counters.</param>
        internal static void RegisterCounters(BasicGameHost target)
        {
            if (FrameStatistics.target != null)
                return;

            FrameStatistics.target = target;

            for (StatisticsCounterType i = 0; i < StatisticsCounterType.AmountTypes; ++i)
                getMonitor(i).RegisterCounter(i);
        }

        internal static void Increment(StatisticsCounterType type, long amount = 1)
        {
            if (target == null)
                return;

            getMonitor(type)?.GetCounter(type)?.Add(amount);
        }

        private static BasicGameHost target;
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

        AmountTypes,
    }
}
