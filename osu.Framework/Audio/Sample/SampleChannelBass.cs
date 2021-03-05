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
        private volatile bool userRequestedPlay;

        public override bool Playing => playing;
        private volatile bool playing;

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
                    // Only unpause if the channel has been played by the user.
                    if (userRequestedPlay)
                        playChannel();
                },
            };

            ensureChannel();
        }

        public override void Play()
        {
            userRequestedPlay = true;

            // Enqueue the playback to start.
            //
            // The playing state set below combined with the call to base.Play() will make this channel visible to the audio thread if it's not already.
            // If the audio thread were to update and see the channel not playing, it would reset the playing state and remove the channel from the audio thread's visibility.
            //
            // In order to prevent this race, playback is enqueued first so that the audio thread is guaranteed to play the channel when it becomes visible to it.
            playChannel();

            // The playing state keeps the channel alive to receive updates from the audio thread. It will not receive updates until base.Play().
            playing = true;

            // Notifies Sample/SampleBassFactory that this channel has come alive.
            base.Play();
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
            if (hasChannel)
            {
                switch (Bass.ChannelIsActive(channel))
                {
                    case PlaybackState.Playing:
                    // Stalled counts as playing, as playback will continue once more data has streamed in.
                    case PlaybackState.Stalled:
                    // The channel is in a "paused" state via zero-frequency. It should be marked as playing even if it's in a paused state internally.
                    case PlaybackState.Paused when userRequestedPlay:
                        playing = true;
                        break;

                    default:
                        playing = false;
                        break;
                }
            }
            else
            {
                // Channel doesn't exist - a rare case occurring as a result of device updates.
                playing = false;
            }

            base.UpdateState();

            bassAmplitudeProcessor?.Update();
        }

        public override void Stop()
        {
            userRequestedPlay = false;

            base.Stop();

            stopChannel();
        }

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

            // Bass will restart the sample if it has reached its end. This behavior isn't desirable so block locally.
            // Unlike TrackBass, sample channels can't have sync callbacks attached, so the stopped state is used instead
            // to indicate the natural stoppage of a sample as a result of having reaching the end.
            if (Played && Bass.ChannelIsActive(channel) == PlaybackState.Stopped)
                return;

            if (relativeFrequencyHandler.IsFrequencyZero)
                return;

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

            if (!hasChannel)
                return;

            Bass.ChannelSetAttribute(channel, ChannelAttribute.NoRamp, 1);
            setLoopFlag(Looping);

            relativeFrequencyHandler.SetChannel(channel);
            bassAmplitudeProcessor?.SetChannel(channel);
        });

        protected override void Dispose(bool disposing)
        {
            if (IsDisposed)
                return;

            if (hasChannel)
            {
                Bass.ChannelStop(channel);
                channel = 0;
            }

            playing = false;

            base.Dispose(disposing);
        }
    }
}
