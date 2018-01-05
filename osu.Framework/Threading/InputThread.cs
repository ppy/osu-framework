// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Statistics;
using System;
using System.Collections.Generic;

namespace osu.Framework.Threading
{
    public class InputThread : GameThread
    {
        public InputThread(Action onNewFrame, string threadName) : base(onNewFrame, threadName)
        {
        }

        internal override IEnumerable<StatisticsCounterType> StatisticsCounters => new[]
        {
            StatisticsCounterType.MouseEvents,
            StatisticsCounterType.KeyEvents,
        };

        public void RunUpdate() => ProcessFrame();
    }
}
