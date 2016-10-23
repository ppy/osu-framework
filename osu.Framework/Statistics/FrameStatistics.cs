// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;

namespace osu.Framework.Statistics
{
    public class FrameStatistics
    {
        internal Dictionary<PerformanceCollectionType, double> CollectedTimes = new Dictionary<PerformanceCollectionType, double>();
        internal Dictionary<PerformanceCounterType, long> Counts = new Dictionary<PerformanceCounterType, long>();
        internal List<int> GarbageCollections = new List<int>();

        internal void Clear()
        {
            CollectedTimes.Clear();
            GarbageCollections.Clear();
            Counts.Clear();
        }

        internal static PerformanceCounterType[] InputCounters => new PerformanceCounterType[]
        {
        };

        internal static PerformanceCounterType[] UpdateCounters => new[]
        {
            PerformanceCounterType.Invalidations,
            PerformanceCounterType.DrawNodeConstructions,
        };

        internal static PerformanceCounterType[] DrawCounters => new[]
        {
            PerformanceCounterType.TextureBinds,
            PerformanceCounterType.BufferDraws,
            PerformanceCounterType.Vertices,
            PerformanceCounterType.Pixels,
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

    public enum PerformanceCounterType
    {
        BufferDraws,
        TextureBinds,
        Invalidations,
        DrawNodeConstructions,
        Vertices,
        Pixels,
    }
}
