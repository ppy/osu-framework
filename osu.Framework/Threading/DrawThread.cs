﻿// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Statistics;
using System;
using System.Collections.Generic;

namespace osu.Framework.Threading
{
    public class DrawThread : GameThread
    {
        public DrawThread(Action onNewFrame, string threadName) : base(onNewFrame, threadName)
        {
        }

        internal override IEnumerable<StatisticsCounterType> StatisticsCounters => new[]
        {
            StatisticsCounterType.VBufBinds,
            StatisticsCounterType.VBufOverflow,
            StatisticsCounterType.TextureBinds,
            StatisticsCounterType.DrawCalls,
            StatisticsCounterType.VerticesDraw,
            StatisticsCounterType.VerticesUpl,
            StatisticsCounterType.Pixels,
        };
    }
}
