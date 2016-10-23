// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;

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

        internal static StatisticsCounterType[] InputCounters => new StatisticsCounterType[]
        {
        };

        internal static StatisticsCounterType[] UpdateCounters => new[]
        {
            StatisticsCounterType.Invalidations,
            StatisticsCounterType.Refreshes,
            StatisticsCounterType.DrawNodeCtor,
        };

        internal static StatisticsCounterType[] DrawCounters => new[]
        {
            StatisticsCounterType.TextureBinds,
            StatisticsCounterType.DrawCalls,
            StatisticsCounterType.Vertices,
        };
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
