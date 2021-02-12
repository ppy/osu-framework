// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using ManagedBass;
using osu.Framework.Audio.Track;

namespace osu.Framework.Audio.Sample
{
    internal sealed class SampleChannelBass : SampleChannel, IBassAudio
    {
        private readonly SampleBass sample;
        private volatile int channel;
        private volatile bool playing;

        private readonly BassRelativeFrequencyHandler relativeFrequencyHandler;
        private BassAmplitudeProcessor bassAmplitudeProcessor;

        public SampleChannelBass(SampleBass sample)
        {
            this.sample = sample;

            relativeFrequencyHandler = new BassRelativeFrequencyHandler
            {
                FrequencyChangedToZero = () => Bass.ChannelPause(channel),
                FrequencyChangedFromZero = () => Bass.ChannelPlay(channel),
            };

            EnqueueAction(() =>
            {
                ensureChannel();

                if (channel == 0)
                    return;

                Bass.ChannelSetAttribute(channel, ChannelAttribute.NoRamp, 1);
                setLoopFlag(Looping);

                relativeFrequencyHandler.SetChannel(channel);
                bassAmplitudeProcessor?.SetChannel(channel);
            });
        }

        public override void Play()
        {
            // Needs to happen on the main thread such that Played does not become true before Playing for a short moment.
            playing = true;

            base.Play();

            EnqueueAction(() =>
            {
                // Channel may have been freed via UpdateDevice().
                ensureChannel();

                if (channel == 0)
                    return;

                // Ensure state is correct before starting.
                InvalidateState();

                if (channel != 0 && !relativeFrequencyHandler.IsFrequencyZero)
                    Bass.ChannelPlay(channel);
            });
        }

        void IBassAudio.UpdateDevice(int deviceIndex)
        {
            // Channels created from samples can not be migrated, so we need to ensure
            // a new channel is created after switching the device. We do not need to
            // manually free the channel, because our Bass.Free call upon switching devices
            // takes care of that.
            channel = 0;
        }

        internal override void OnStateChanged()
        {
            base.OnStateChanged();

            if (channel == 0)
                return;

            Bass.ChannelSetAttribute(channel, ChannelAttribute.Volume, AggregateVolume.Value);
            Bass.ChannelSetAttribute(channel, ChannelAttribute.Pan, AggregateBalance.Value);
            relativeFrequencyHandler.SetFrequency(AggregateFrequency.Value);
        }

        public override bool Looping
        {
            get => base.Looping;
            set
            {
                base.Looping = value;
                setLoopFlag(Looping);
            }
        }

        protected override void UpdateState()
        {
            // Initial playing state depending on whether a channel exists or not. This should be true in most cases.
            playing = channel != 0;

            // Adjust playing state depending on whether the channel is actually playing. Stalled counts as playing, as playback will continue once more data is streamed in.
            var state = Bass.ChannelIsActive(channel);
            playing = playing && (state == PlaybackState.Playing || state == PlaybackState.Stalled);

            base.UpdateState();

            bassAmplitudeProcessor?.Update();
        }

        public override void Stop()
        {
            base.Stop();

            EnqueueAction(() =>
            {
                if (channel == 0)
                    return;

                Bass.ChannelPause(channel);
            });
        }

        public override bool Playing => playing;

        public override ChannelAmplitudes CurrentAmplitudes => (bassAmplitudeProcessor ??= new BassAmplitudeProcessor(channel)).CurrentAmplitudes;

        private void setLoopFlag(bool value) => EnqueueAction(() =>
        {
            if (channel != 0)
                Bass.ChannelFlags(channel, value ? BassFlags.Loop : BassFlags.Default, BassFlags.Loop);
        });

        private void ensureChannel() => EnqueueAction(() =>
        {
            if (channel != 0)
                return;

            channel = Bass.SampleGetChannel(sample.SampleId);
        });
    }
}
