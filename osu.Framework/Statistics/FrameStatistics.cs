// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;

namespace osu.Framework.Statistics
{
    internal class FrameStatistics
    {
        internal readonly Dictionary<PerformanceCollectionType, double> CollectedTimes = new Dictionary<PerformanceCollectionType, double>(NUM_STATISTICS_COUNTER_TYPES);
        internal readonly Dictionary<StatisticsCounterType, long> Counts = new Dictionary<StatisticsCounterType, long>(NUM_STATISTICS_COUNTER_TYPES);
        internal readonly List<int> GarbageCollections = new List<int>();

        internal static readonly int NUM_STATISTICS_COUNTER_TYPES = Enum.GetValues(typeof(StatisticsCounterType)).Length;
        internal static readonly int NUM_PERFORMANCE_COLLECTION_TYPES = Enum.GetValues(typeof(PerformanceCollectionType)).Length;

        internal static readonly long[] COUNTERS = new long[NUM_STATISTICS_COUNTER_TYPES];

        internal void Clear()
        {
            CollectedTimes.Clear();
            GarbageCollections.Clear();
            Counts.Clear();
        }

        internal static void Increment(StatisticsCounterType type) => ++COUNTERS[(int)type];

        internal static void Add(StatisticsCounterType type, long amount) => COUNTERS[(int)type] += amount;
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
        Pixels,

        TasksRun,
        Tracks,
        Samples,
        SChannels,
        Components,

        MouseEvents,
        KeyEvents,
        JoystickEvents,
    }
}
