// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Statistics;
using System;
using System.Collections.Generic;
using osu.Framework.Development;

namespace osu.Framework.Threading
{
    public class DrawThread : GameThread
    {
        public DrawThread(Action onNewFrame)
            : base(onNewFrame, "Draw")
        {
        }

        public override bool IsCurrent => ThreadSafety.IsDrawThread;

        internal override void MakeCurrent()
        {
            base.MakeCurrent();
            ThreadSafety.IsDrawThread = true;
        }

        internal override IEnumerable<StatisticsCounterType> StatisticsCounters => new[]
        {
            StatisticsCounterType.VBufBinds,
            StatisticsCounterType.VBufOverflow,
            StatisticsCounterType.TextureBinds,
            StatisticsCounterType.FBORedraw,
            StatisticsCounterType.DrawCalls,
            StatisticsCounterType.ShaderBinds,
            StatisticsCounterType.VerticesDraw,
            StatisticsCounterType.VerticesUpl,
            StatisticsCounterType.Pixels,
        };
    }
}
