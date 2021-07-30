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
    internal class AudioMixerOverlay : ToolWindow
    {
        [Resolved]
        private AudioManager audioManager { get; set; }

        private readonly FillFlowContainer<MixerVisualiser> mixerFlow;
        private readonly IBindableList<int> activeMixerHandles = new BindableList<int>();

        public AudioMixerOverlay()
            : base("AudioMixer", "(Ctrl+F9 to toggle)")
        {
            ScrollContent.Expire();
            MainHorizontalContent.Add(new BasicScrollContainer(Direction.Horizontal)
            {
                RelativeSizeAxes = Axes.Y,
                Width = WIDTH,
                Children = new[]
                {
                    mixerFlow = new FillFlowContainer<MixerVisualiser>
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
                    foreach (var handle in e.NewItems.OfType<int>())
                        mixerFlow.Add(new MixerVisualiser(handle));
                    break;

                case NotifyCollectionChangedAction.Remove:
                    Debug.Assert(e.OldItems != null);
                    mixerFlow.RemoveAll(m => !e.OldItems.OfType<int>().Contains(m.MixerHandle));
                    break;
            }
        });

        protected override void Update()
        {
            base.Update();

            foreach (var mixer in audioManager.ActiveMixerHandles)
            {
                if (mixerFlow.All(m => m.MixerHandle != mixer))
                    mixerFlow.Add(new MixerVisualiser(mixer));
            }

            mixerFlow.RemoveAll(m => !audioManager.ActiveMixerHandles.Contains(m.MixerHandle));
        }
    }
}
