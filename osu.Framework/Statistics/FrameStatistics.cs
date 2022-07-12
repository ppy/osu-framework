// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;

namespace osu.Framework.Statistics
{
    internal class FrameStatistics
    {
        internal readonly Dictionary<PerformanceCollectionType, double> CollectedTimes = new Dictionary<PerformanceCollectionType, double>(NUM_STATISTICS_COUNTER_TYPES);
        internal readonly Dictionary<StatisticsCounterType, long> Counts = new Dictionary<StatisticsCounterType, long>(NUM_STATISTICS_COUNTER_TYPES);
        internal readonly List<int> GarbageCollections = new List<int>();
        public double FramesPerSecond { get; set; }

        internal static readonly int NUM_STATISTICS_COUNTER_TYPES = Enum.GetValues(typeof(StatisticsCounterType)).Length;
        internal static readonly int NUM_PERFORMANCE_COLLECTION_TYPES = Enum.GetValues(typeof(PerformanceCollectionType)).Length;

        internal static readonly long[] COUNTERS = new long[NUM_STATISTICS_COUNTER_TYPES];

        internal void Clear()
        {
            CollectedTimes.Clear();
            GarbageCollections.Clear();
            Counts.Clear();
            FramesPerSecond = 0;
        }

        internal static void Increment(StatisticsCounterType type) => ++COUNTERS[(int)type];

        internal static void Add(StatisticsCounterType type, long amount)
        {
            if (amount < 0)
                throw new ArgumentException($"Statistics counter {type} was attempted to be decremented via {nameof(Add)} call.", nameof(amount));

            COUNTERS[(int)type] += amount;
        }
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
        InputQueue,
        PositionalIQ,

        /// <summary>
        /// See <see cref="Graphics.Containers.CompositeDrawable.CheckChildrenLife"/>.
        /// </summary>
        CCL,

        VBufBinds,
        VBufOverflow,
        TextureBinds,
        FBORedraw,
        DrawCalls,
        ShaderBinds,
        VerticesDraw,
        VerticesUpl,
        Pixels,

        TasksRun,
        Tracks,
        Samples,
        SChannels,
        Components,
        MixChannels,

        MouseEvents,
        KeyEvents,
        JoystickEvents,
        MidiEvents,
        TabletEvents,
        TouchEvents,
    }
}
