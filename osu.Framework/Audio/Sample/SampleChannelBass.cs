// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using ManagedBass;
using osu.Framework.Audio.Track;

namespace osu.Framework.Audio.Sample
{
    internal sealed class SampleChannelBass : SampleChannel, IBassAudio
    {
        private volatile int channel;
        private volatile bool playing;

        private readonly BassRelativeFrequencyHandler relativeFrequencyHandler;
        private BassAmplitudeProcessor bassAmplitudeProcessor;

        public SampleChannelBass(SampleBass sample)
        {
            relativeFrequencyHandler = new BassRelativeFrequencyHandler
            {
                FrequencyChangedToZero = () => Bass.ChannelPause(channel),
                FrequencyChangedFromZero = () => Bass.ChannelPlay(channel),
            };

            EnqueueAction(() =>
            {
                channel = Bass.SampleGetChannel(sample.SampleId);
                if (channel == 0)
                    return;

                Bass.ChannelSetAttribute(channel, ChannelAttribute.NoRamp, 1);
                setLoopFlag(Looping);

                relativeFrequencyHandler.SetChannel(channel);
                bassAmplitudeProcessor?.SetChannel(channel);

                // ensure state is correct before starting.
                InvalidateState();

                if (channel != 0 && !relativeFrequencyHandler.IsFrequencyZero)
                    Bass.ChannelPlay(channel, true);
            });

            // Needs to happen on the main thread such that
            // Played does not become true for a short moment.
            playing = true;
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
            playing = channel != 0 && Bass.ChannelIsActive(channel) != 0;

            base.UpdateState();

            bassAmplitudeProcessor?.Update();
        }

        public override void Stop()
        {
            base.Stop();

            EnqueueAction(() =>
            {
                if (channel == 0) return;

                Bass.ChannelStop(channel);

                // ChannelStop frees the channel.
                channel = 0;
            });
        }

        public override bool Playing => playing;

        public override ChannelAmplitudes CurrentAmplitudes => (bassAmplitudeProcessor ??= new BassAmplitudeProcessor(channel)).CurrentAmplitudes;

        private void setLoopFlag(bool value) => EnqueueAction(() =>
        {
            if (channel != 0)
                Bass.ChannelFlags(channel, value ? BassFlags.Loop : BassFlags.Default, BassFlags.Loop);
        });
    }
}
