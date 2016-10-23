// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Platform;
using osu.Framework.Threading;
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

        internal static HashSet<StatisticsCounterType> InputCounters => new HashSet<StatisticsCounterType>
        {
        };

        internal static HashSet<StatisticsCounterType> UpdateCounters => new HashSet < StatisticsCounterType >
        {
            StatisticsCounterType.Invalidations,
            StatisticsCounterType.Refreshes,
            StatisticsCounterType.DrawNodeCtor,
        };

        internal static HashSet<StatisticsCounterType> DrawCounters => new HashSet<StatisticsCounterType>
        {
            StatisticsCounterType.TextureBinds,
            StatisticsCounterType.DrawCalls,
            StatisticsCounterType.Vertices,
        };

        internal static void Increment(StatisticsCounterType type, long amount = 1)
        {
            BasicGameHost host = BasicGameHost.GetInstanceIfExists();
            if (host == null)
                return;

            AtomicCounter counter = null;
            if (UpdateCounters.Contains(type))
                counter = host.UpdateMonitor.GetCounter(type);
            else if (DrawCounters.Contains(type))
                counter = host.DrawMonitor.GetCounter(type);
            else if (InputCounters.Contains(type))
                counter = host.InputMonitor.GetCounter(type);
            else
                Debug.Assert(false, "Requested counter which is not assigned to any performance monitor.");

            counter.Add(amount);
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
