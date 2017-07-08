// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;

namespace osu.Framework.Statistics
{
    internal class FrameStatistics
    {
        internal readonly Dictionary<PerformanceCollectionType, double> CollectedTimes = new Dictionary<PerformanceCollectionType, double>(NumStatisticsCounterTypes);
        internal readonly Dictionary<StatisticsCounterType, long> Counts = new Dictionary<StatisticsCounterType, long>(NumStatisticsCounterTypes);
        internal readonly List<int> GarbageCollections = new List<int>();

        internal static int NumStatisticsCounterTypes = Enum.GetValues(typeof(StatisticsCounterType)).Length;
        internal static int NumPerformanceCollectionTypes = Enum.GetValues(typeof(PerformanceCollectionType)).Length;

        internal static readonly long[] COUNTERS = new long[NumStatisticsCounterTypes];

        internal void Clear()
        {
            CollectedTimes.Clear();
            GarbageCollections.Clear();
            Counts.Clear();
        }

        internal void Postprocess()
        {
            if (Counts.ContainsKey(StatisticsCounterType.KiloPixels))
                Counts[StatisticsCounterType.KiloPixels] /= 1000;
        }

        public static void Increment(StatisticsCounterType type) => ++COUNTERS[(int)type];

        public static void Add(StatisticsCounterType type, long amount) => COUNTERS[(int)type] += amount;
    }

    internal enum PerformanceCollectionType
    {
        Work = 0,
        SwapBuffer,
        WndProc,
        Debug,
        Sleep,
        Scheduler,
        IPC,
        GLReset,
    }

    internal enum StatisticsCounterType
    {
        Invalidations = 0,
        Refreshes,
        DrawNodeCtor,
        DrawNodeAppl,
        ScheduleInvk,

        VBufBinds,
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

        MouseEvents,
        KeyEvents,
    }
}
