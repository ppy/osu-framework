// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Graphics.Containers;
using osuTK;

namespace osu.Framework.Graphics.Visualisation.Audio
{
    internal class AudioMixerVisualiser : ToolWindow
    {
        [Resolved]
        private AudioManager audioManager { get; set; }

        private readonly FillFlowContainer<MixerDisplay> mixerFlow;

        public AudioMixerVisualiser()
            : base("AudioMixer", "(Ctrl+F9 to toggle)")
        {
            ScrollContent.Expire();
            MainHorizontalContent.Add(new BasicScrollContainer(Direction.Horizontal)
            {
                RelativeSizeAxes = Axes.Y,
                Width = WIDTH * 2,
                Children = new[]
                {
                    mixerFlow = new FillFlowContainer<MixerDisplay>
                    {
                        RelativeSizeAxes = Axes.Y,
                        AutoSizeAxes = Axes.X,
                        Spacing = new Vector2(10),
                        Padding = new MarginPadding(10)
                    }
                }
            });
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            mixerFlow.Add(new MixerDisplay(audioManager.OutputMixer));
        }
    }
}
