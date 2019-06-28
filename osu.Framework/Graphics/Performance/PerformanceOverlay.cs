// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Containers;
using osu.Framework.Threading;
using System.Collections.Generic;

namespace osu.Framework.Graphics.Performance
{
    internal class PerformanceOverlay : FillFlowContainer<FrameStatisticsDisplay>, IStateful<FrameStatisticsMode>
    {
        private readonly IEnumerable<GameThread> threads;
        private FrameStatisticsMode state;

        public event Action<FrameStatisticsMode> StateChanged;

        private bool initialised;

        public FrameStatisticsMode State
        {
            get => state;
            set
            {
                if (state == value) return;

                state = value;

                switch (state)
                {
                    case FrameStatisticsMode.None:
                        this.FadeOut(100);
                        break;

                    case FrameStatisticsMode.Minimal:
                    case FrameStatisticsMode.Full:
                        if (!initialised)
                        {
                            initialised = true;
                            foreach (GameThread t in threads)
                                Add(new FrameStatisticsDisplay(t) { State = state });
                        }

                        this.FadeIn(100);
                        break;
                }

                foreach (FrameStatisticsDisplay d in Children)
                    d.State = state;

                StateChanged?.Invoke(State);
            }
        }

        public PerformanceOverlay(IEnumerable<GameThread> threads)
        {
            this.threads = threads;
            Direction = FillDirection.Vertical;
        }
    }

    public enum FrameStatisticsMode
    {
        None,
        Minimal,
        Full
    }
}
