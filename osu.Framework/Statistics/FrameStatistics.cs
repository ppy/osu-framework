// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Platform;
using osu.Framework.Threading;
using System;
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
            BasicGameHost host = BasicGameHost.GetInstanceIfExists();
            if (host == null)
                return null;

            switch (type)
            {
                case StatisticsCounterType.Invalidations:
                case StatisticsCounterType.Refreshes:
                case StatisticsCounterType.DrawNodeCtor:
                    return host.UpdateMonitor;

                case StatisticsCounterType.TextureBinds:
                case StatisticsCounterType.DrawCalls:
                case StatisticsCounterType.Vertices:
                    return host.DrawMonitor;

                default:
                    Debug.Assert(false, "Requested counter which is not assigned to any performance monitor.");
                    break;
            }

            return null;
        }

        internal static void RegisterCounters()
        {
            foreach (StatisticsCounterType type in Enum.GetValues(typeof(StatisticsCounterType)))
                getMonitor(type)?.RegisterCounter(type);
        }

        internal static void Increment(StatisticsCounterType type, long amount = 1)
        {
            getMonitor(type)?.GetCounter(type).Add(amount);
        }
    }

    public enum PerformanceCollectionType
    {
        Update,
        Draw,
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
        DrawCalls,
        TextureBinds,
        Invalidations,
        Refreshes,
        DrawNodeCtor,
        Vertices,
    }
}
