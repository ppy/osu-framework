// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;

namespace osu.Framework.Statistics
{
    public class FrameStatistics
    {
        internal readonly Dictionary<PerformanceCollectionType, double> CollectedTimes = new Dictionary<PerformanceCollectionType, double>((int)PerformanceCollectionType.AmountTypes);
        internal readonly Dictionary<StatisticsCounterType, long> Counts = new Dictionary<StatisticsCounterType, long>((int)StatisticsCounterType.AmountTypes);
        internal readonly List<int> GarbageCollections = new List<int>();

        internal static readonly long[] COUNTERS = new long[(int)StatisticsCounterType.AmountTypes];

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

        public static void Increment(StatisticsCounterType type, long amount = 1) => ++COUNTERS[(int)type];
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
        AmountTypes,
    }

    public enum StatisticsCounterType
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

        AmountTypes,
    }
}
