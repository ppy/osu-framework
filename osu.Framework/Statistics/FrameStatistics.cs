//Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
//Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Diagnostics.PerformanceData;

namespace osu.Framework.Statistics
{
    public class FrameStatistics
    {
        internal Dictionary<FrameTimeType, double> CollectedTimes = new Dictionary<FrameTimeType, double>();
        internal Dictionary<CounterType, int> CollectedCounters = new Dictionary<CounterType, int>();
        internal List<int> GarbageCollections = new List<int>();
    }

    public enum FrameTimeType
    {
        Update,
        Draw,
        SwapBuffer,
        BetweenFrames,
        Debug,
        Sleep,
        Scheduler,
        IPC,
        Empty
    }
}
