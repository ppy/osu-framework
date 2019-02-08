// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Statistics;
using System;
using System.Collections.Generic;

namespace osu.Framework.Threading
{
    public class InputThread : GameThread
    {
        public InputThread(Action onNewFrame)
            : base(onNewFrame, "Input")
        {
        }

        internal override IEnumerable<StatisticsCounterType> StatisticsCounters => new[]
        {
            StatisticsCounterType.MouseEvents,
            StatisticsCounterType.KeyEvents,
            StatisticsCounterType.JoystickEvents,
        };

        public void RunUpdate() => ProcessFrame();
    }
}
