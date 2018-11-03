// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.Graphics.Textures;
using OpenTK.Graphics.ES30;
using osu.Framework.Threading;
using System.Collections.Generic;

namespace osu.Framework.Graphics.Performance
{
    internal class PerformanceOverlay : FillFlowContainer<FrameStatisticsDisplay>, IState<PerformanceOverlayState>
    {
        private PerformanceOverlayState state;
        public PerformanceOverlayState State
        {
            get => state;
            set
            {
                state = value;
                StateChanged?.Invoke(value);
            }
        }

        public event Action<PerformanceOverlayState> StateChanged;

        public PerformanceOverlay(IEnumerable<GameThread> threads)
        {
            State = new PerformanceOverlayState.None(this);
            Direction = FillDirection.Vertical;
            TextureAtlas atlas = new TextureAtlas(GLWrapper.MaxTextureSize, GLWrapper.MaxTextureSize, true, All.Nearest);

            foreach (GameThread t in threads)
            {
                var frameStatisticsDisplay = new FrameStatisticsDisplay(t, atlas);
                frameStatisticsDisplay.State = State.CreateFrameStatisticsDisplayState(frameStatisticsDisplay);
                Add(frameStatisticsDisplay);
            }
        }
    }
}
