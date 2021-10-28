// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Bindables;
using osu.Framework.Graphics.Containers;
using osuTK;

namespace osu.Framework.Graphics.Visualisation.Audio
{
    internal class AudioMixerVisualiser : ToolWindow
    {
        [Resolved]
        private AudioManager audioManager { get; set; }

        private readonly FillFlowContainer<MixerDisplay> mixerFlow;
        private readonly IBindableList<int> activeMixerHandles = new BindableList<int>();

        public AudioMixerVisualiser()
            : base("AudioMixer", "(Ctrl+F9 to toggle)")
        {
            ScrollContent.Expire();
            MainHorizontalContent.Add(new BasicScrollContainer(Direction.Horizontal)
            {
                RelativeSizeAxes = Axes.Y,
                Width = WIDTH,
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

            activeMixerHandles.BindTo(audioManager.ActiveMixerHandles);
            activeMixerHandles.BindCollectionChanged(onActiveMixerHandlesChanged, true);
        }

        private void onActiveMixerHandlesChanged(object sender, NotifyCollectionChangedEventArgs e) => Schedule(() =>
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    Debug.Assert(e.NewItems != null);
                    foreach (int handle in e.NewItems.OfType<int>())
                        mixerFlow.Add(new MixerDisplay(handle));
                    break;

                case NotifyCollectionChangedAction.Remove:
                    Debug.Assert(e.OldItems != null);
                    mixerFlow.RemoveAll(m => e.OldItems.OfType<int>().Contains(m.MixerHandle));
                    break;
            }
        });
    }
}
