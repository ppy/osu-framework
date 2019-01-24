// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Statistics;
using System;
using System.Collections.Generic;

namespace osu.Framework.Threading
{
    public class UpdateThread : GameThread
    {
        public UpdateThread(Action onNewFrame)
            : base(onNewFrame, "Update")
        {
        }

        internal override IEnumerable<StatisticsCounterType> StatisticsCounters => new[]
        {
            StatisticsCounterType.Invalidations,
            StatisticsCounterType.Refreshes,
            StatisticsCounterType.DrawNodeCtor,
            StatisticsCounterType.DrawNodeAppl,
            StatisticsCounterType.ScheduleInvk,
            StatisticsCounterType.InputQueue,
            StatisticsCounterType.PositionalIQ,
            StatisticsCounterType.CCL
        };
    }
}
