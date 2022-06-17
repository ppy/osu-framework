// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Audio.Mixing;
using osu.Framework.Audio.Sample;
using osu.Framework.Bindables;
using osu.Framework.Lists;

namespace osu.Framework.Graphics.Audio
{
    /// <summary>
    /// A <see cref="SampleChannel"/> wrapper to allow insertion in the draw hierarchy to allow transforms, lifetime management etc.
    /// </summary>
    public class DrawableSample : DrawableAudioWrapper, ISample
    {
        private readonly WeakList<SampleChannel> playingChannels = new WeakList<SampleChannel>();

        private readonly ISample sample;

        /// <summary>
        /// Construct a new drawable sample instance.
        /// </summary>
        /// <param name="sample">The audio sample to wrap.</param>
        /// <param name="disposeSampleOnDisposal">Whether the sample should be automatically disposed on drawable disposal/expiry.</param>
        public DrawableSample(ISample sample, bool disposeSampleOnDisposal = true)
            : base(sample, disposeSampleOnDisposal)
        {
            this.sample = sample;

            PlaybackConcurrency.BindTo(sample.PlaybackConcurrency);
        }

        public SampleChannel Play()
        {
            var channel = GetChannel();
            channel.Play();
            return channel;
        }

        public SampleChannel GetChannel()
        {
            var channel = sample.GetChannel();

            playingChannels.Add(channel);
            mixer?.Add(channel);

            return channel;
        }

        public double Length => sample.Length;

        public Bindable<int> PlaybackConcurrency { get; } = new Bindable<int>(Sample.DEFAULT_CONCURRENCY);

        private IAudioMixer? mixer;

        protected override void OnMixerChanged(ValueChangedEvent<IAudioMixer> mixer)
        {
            base.OnMixerChanged(mixer);

            this.mixer = mixer.NewValue;

            foreach (var channel in playingChannels)
            {
                mixer.OldValue?.Remove(channel);
                mixer.NewValue?.Add(channel);
            }
        }
    }
}
