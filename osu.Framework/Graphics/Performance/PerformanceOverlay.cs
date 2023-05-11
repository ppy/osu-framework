// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Buffers;
using osu.Framework.Graphics.Containers;
using osu.Framework.Threading;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Platform;
using SixLabors.ImageSharp.PixelFormats;

namespace osu.Framework.Graphics.Performance
{
    internal partial class PerformanceOverlay : FillFlowContainer, IStateful<FrameStatisticsMode>
    {
        [Resolved]
        private GameHost host { get; set; } = null!;

        private FrameStatisticsMode state;

        public event Action<FrameStatisticsMode>? StateChanged;

        private bool initialised;

        public FrameStatisticsMode State
        {
            get => state;
            set
            {
                if (state == value) return;

                state = value;

                if (IsLoaded)
                    updateState();
            }
        }

        public PerformanceOverlay()
        {
            Direction = FillDirection.Vertical;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            updateState();
        }

        private void updateState()
        {
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

                        var uploadPool = createUploadPool();

                        Add(new SpriteText
                        {
                            Text = $"Renderer: {host.RendererInfo}",
                            Alpha = 0.75f,
                            Origin = Anchor.TopRight,
                        });

                        foreach (GameThread t in host.Threads)
                            Add(new FrameStatisticsDisplay(t, uploadPool) { State = state });
                    }

                    this.FadeIn(100);
                    break;
            }

            foreach (FrameStatisticsDisplay d in Children.OfType<FrameStatisticsDisplay>())
                d.State = state;

            StateChanged?.Invoke(State);
        }

        private ArrayPool<Rgba32> createUploadPool()
        {
            // bucket size should be enough to allow some overhead when running multi-threaded with draw at 60hz.
            const int max_expected_thread_update_rate = 2000;

            int bucketSize = host.Threads.Count() * (max_expected_thread_update_rate / 60);

            return ArrayPool<Rgba32>.Create(FrameStatisticsDisplay.HEIGHT, bucketSize);
        }
    }

    public enum FrameStatisticsMode
    {
        None,
        Minimal,
        Full
    }
}
