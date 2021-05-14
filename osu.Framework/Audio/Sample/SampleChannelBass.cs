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

        /// <summary>
        /// Whether the channel is currently playing.
        /// </summary>
        /// <remarks>
        /// This is set to <c>true</c> immediately upon <see cref="Play"/>, but the channel may not be audibly playing yet.
        /// </remarks>
        public override bool Playing => playing || enqueuedPlaybackStart;

        private volatile bool playing;

        /// <summary>
        /// <c>true</c> if the user last called <see cref="Play"/>.
        /// <c>false</c> if the user last called <see cref="Stop"/>.
        /// </summary>
        private volatile bool userRequestedPlay;

        /// <summary>
        /// Whether the playback start has been enqueued.
        /// </summary>
        private volatile bool enqueuedPlaybackStart;

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

            // Pin Playing and IsAlive to true so that the channel isn't killed by the next update. This is only reset after playback is started.
            enqueuedPlaybackStart = true;

            // Bring this channel alive, allowing it to receive updates.
            base.Play();

            playChannel();
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
            try
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

                playing = true;

                if (!relativeFrequencyHandler.IsFrequencyZero)
                    Bass.ChannelPlay(channel);
            }
            finally
            {
                enqueuedPlaybackStart = false;
            }
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
