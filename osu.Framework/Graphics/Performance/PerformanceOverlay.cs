// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.Containers;
using System.Linq;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.Graphics.Textures;
using OpenTK.Graphics.ES30;
using osu.Framework.Threading;

namespace osu.Framework.Graphics.Performance
{
    class PerformanceOverlay : FlowContainer, IStateful<FrameStatisticsMode>
    {
        TextureAtlas atlas;

        private FrameStatisticsMode state;

        public FrameStatisticsMode State
        {
            get
            {
                return state;
            }

            set
            {
                if (state == value) return;

                state = value;

                switch (state)
                {
                    case FrameStatisticsMode.None:
                        FadeOut(100);
                        break;
                    case FrameStatisticsMode.Minimal:
                    case FrameStatisticsMode.Full:
                        FadeIn(100);
                        break;
                }

                foreach (FrameStatisticsDisplay d in Children.Cast<FrameStatisticsDisplay>())
                    d.State = state;
            }
        }

        public void AddThread(GameThread thread) => Add(new FrameStatisticsDisplay(thread, atlas));

        public PerformanceOverlay()
        {
            Direction = FlowDirections.Vertical;
            atlas = new TextureAtlas(GLWrapper.MaxTextureSize, GLWrapper.MaxTextureSize, true, All.Nearest);
        }
    }

    public enum FrameStatisticsMode
    {
        None,
        Minimal,
        Full
    }
}
