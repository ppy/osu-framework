// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Graphics.Containers;
using System.Linq;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.Graphics.Textures;

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

        public override void Load(BaseGame game)
        {
            base.Load(game);

            atlas = new TextureAtlas(GLWrapper.MaxTextureSize, GLWrapper.MaxTextureSize, true);

            Add(new FrameStatisticsDisplay(@"Input", game.Host.InputMonitor, atlas));
            Add(new FrameStatisticsDisplay(@"Update", game.Host.UpdateMonitor, atlas));
            Add(new FrameStatisticsDisplay(@"Draw", game.Host.DrawMonitor, atlas));

            Direction = FlowDirection.VerticalOnly;
        }
    }

    public enum FrameStatisticsMode
    {
        None,
        Minimal,
        Full
    }
}
