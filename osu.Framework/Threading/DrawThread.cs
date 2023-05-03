// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Statistics;
using System;
using System.Collections.Generic;
using osu.Framework.Development;
using osu.Framework.Platform;
using osuTK;

namespace osu.Framework.Threading
{
    public class DrawThread : GameThread
    {
        private readonly GameHost host;

        public DrawThread(Action onNewFrame, GameHost host)
            : base(onNewFrame, "Draw")
        {
            this.host = host;
        }

        public override bool IsCurrent => ThreadSafety.IsDrawThread;

        protected sealed override void OnInitialize()
        {
            var window = host.Window;

            if (window != null)
            {
                host.Renderer.BeginFrame(new Vector2(window.ClientSize.Width, window.ClientSize.Height));
                host.Renderer.FinishFrame();
            }
        }

        internal sealed override void MakeCurrent()
        {
            base.MakeCurrent();

            ThreadSafety.IsDrawThread = true;
        }

        protected sealed override void OnSuspended()
        {
            base.OnSuspended();

            // if we've acquired the GL context before in this thread, make sure to release it before suspension.
            if (host.Renderer.IsInitialised)
                host.Renderer.ClearCurrent();
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
            StatisticsCounterType.UniformUpl,
            StatisticsCounterType.Pixels,
        };
    }
}
