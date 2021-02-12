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
        private volatile bool stopped = true;

        private readonly BassRelativeFrequencyHandler relativeFrequencyHandler;
        private BassAmplitudeProcessor bassAmplitudeProcessor;

        public SampleChannelBass(SampleBass sample)
        {
            this.sample = sample;

            relativeFrequencyHandler = new BassRelativeFrequencyHandler
            {
                FrequencyChangedToZero = stopChannel,
                FrequencyChangedFromZero = () =>
                {
                    // Only unpause if not stopped manually.
                    if (!stopped)
                        playChannel();
                },
            };

            ensureChannel();
        }

        public override void Play()
        {
            base.Play();

            playChannel();

            playing = true; // Needs to happen on the main thread such that Played does not become true before Playing for a short moment.
            stopped = false;
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

            if (!hasChannel)
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
            playing = hasChannel;

            // Adjust playing state depending on whether the channel is actually playing. Stalled counts as playing, as playback will continue once more data is streamed in.
            if (playing)
            {
                var state = Bass.ChannelIsActive(channel);
                playing = state == PlaybackState.Playing || state == PlaybackState.Stalled;
            }

            base.UpdateState();

            bassAmplitudeProcessor?.Update();
        }

        public override void Stop()
        {
            base.Stop();

            stopChannel();

            stopped = true;
        }

        public override bool Playing => playing;

        public override ChannelAmplitudes CurrentAmplitudes => (bassAmplitudeProcessor ??= new BassAmplitudeProcessor(channel)).CurrentAmplitudes;

        private bool hasChannel => channel != 0;

        private void playChannel() => EnqueueAction(() =>
        {
            // Channel may have been freed via UpdateDevice().
            ensureChannel();

            if (!hasChannel)
                return;

            // Ensure state is correct before starting.
            InvalidateState();

            if (hasChannel && !relativeFrequencyHandler.IsFrequencyZero)
                Bass.ChannelPlay(channel);
        });

        private void stopChannel() => EnqueueAction(() =>
        {
            if (hasChannel)
                Bass.ChannelPause(channel);
        });

        private void setLoopFlag(bool value) => EnqueueAction(() =>
        {
            if (hasChannel)
                Bass.ChannelFlags(channel, value ? BassFlags.Loop : BassFlags.Default, BassFlags.Loop);
        });

        private void ensureChannel() => EnqueueAction(() =>
        {
            if (hasChannel)
                return;

            channel = Bass.SampleGetChannel(sample.SampleId);

            Bass.ChannelSetAttribute(channel, ChannelAttribute.NoRamp, 1);
            setLoopFlag(Looping);

            relativeFrequencyHandler.SetChannel(channel);
            bassAmplitudeProcessor?.SetChannel(channel);
        });
    }
}
