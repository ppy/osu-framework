// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Allocation;
using osu.Framework.Audio.Sample;
using osu.Framework.Audio.Track;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Audio;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Visualisation.Audio;

namespace osu.Framework.Tests.Visual.Testing
{
    public class TestSceneAudioMixerVisualiser : FrameworkTestScene
    {
        private Container<TestAudioPlayingSource> mixedSources = new Container<TestAudioPlayingSource>();
        private TestAudioPlayingSource globalSource;

        [BackgroundDependencyLoader]
        private void load()
        {
            AudioMixerVisualiser visualiser;

            Children = new Drawable[]
            {
                globalSource = new TestAudioPlayingSource(false),
                mixedSources = new Container<TestAudioPlayingSource>(),
                visualiser = new AudioMixerVisualiser()
            };

            visualiser.Show();
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            addButtonsForSource("global", globalSource);

            AddStep("add mixer", () =>
            {
                var source = new TestAudioPlayingSource(true);
                mixedSources.Add(source);
                addButtonsForSource($"mixer {mixedSources.Count}", source);
            });
        }

        private void addButtonsForSource(string name, TestAudioPlayingSource source)
        {
            AddStep($"play track on {name}", source.PlayTrack);
            AddStep($"stop track on {name}", source.StopTrack);
            AddStep($"play sample on {name}", source.PlaySample);

            if (source != globalSource)
                AddStep($"remove mixer {name}", () => source.Expire());
        }

        private class TestAudioPlayingSource : CompositeDrawable
        {
            private readonly bool withMixer;
            private DrawableTrack track;
            private DrawableSample sample;

            public TestAudioPlayingSource(bool withMixer)
            {
                this.withMixer = withMixer;
            }

            [BackgroundDependencyLoader]
            private void load(ITrackStore tracks, ISampleStore samples)
            {
                track = new DrawableTrack(tracks.Get("sample-track.mp3"));
                sample = new DrawableSample(samples.Get("long.mp3"));

                if (withMixer)
                {
                    InternalChild = new DrawableAudioMixer
                    {
                        Name = "drawable mixer",
                        Children = new Drawable[] { track, sample }
                    };
                }
                else
                    InternalChildren = new Drawable[] { track, sample };
            }

            public void PlayTrack() => Schedule(() => track.Start());
            public void StopTrack() => Schedule(() => track.Stop());
            public void PlaySample() => Schedule(() => sample.Play());
        }
    }
}
