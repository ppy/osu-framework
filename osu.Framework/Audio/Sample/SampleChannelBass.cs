// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using ManagedBass;
using osu.Framework.Audio.Track;
using osuTK;

namespace osu.Framework.Audio.Sample
{
    public sealed class SampleChannelBass : SampleChannel, IBassAudio
    {
        private volatile int channel;
        private volatile bool playing;

        public override bool IsLoaded => Sample.IsLoaded;

        private float initialFrequency;

        private BassAmplitudeProcessor bassAmplitudeProcessor;

        public SampleChannelBass(Sample sample, Action<SampleChannel> onPlay)
            : base(sample, onPlay)
        {
        }

        void IBassAudio.UpdateDevice(int deviceIndex)
        {
            // Channels created from samples can not be migrated, so we need to ensure
            // a new channel is created after switching the device. We do not need to
            // manually free the channel, because our Bass.Free call upon switching devices
            // takes care of that.
            channel = 0;
        }

        private bool pausedDueToZeroFrequency;

        internal override void OnStateChanged()
        {
            base.OnStateChanged();

            if (channel == 0)
                return;

            Bass.ChannelSetAttribute(channel, ChannelAttribute.Volume, AggregateVolume.Value);
            Bass.ChannelSetAttribute(channel, ChannelAttribute.Pan, AggregateBalance.Value);
            Bass.ChannelSetAttribute(channel, ChannelAttribute.Frequency, bassFreq);

            var channelState = Bass.ChannelIsActive(channel);

            // Handle channels with 0 frequencies due to BASS not supporting them (0 = original rate)
            // Documentation for the frequency limits: http://bass.radio42.com/help/html/ff7623f0-6e9f-6be8-c8a7-17d3a6dc6d51.htm
            if (AggregateFrequency.Value == 0 && channelState != PlaybackState.Paused)
            {
                Debug.Assert(!pausedDueToZeroFrequency, $"{nameof(pausedDueToZeroFrequency)} set while channel is playing.");

                Bass.ChannelPause(channel);
                pausedDueToZeroFrequency = true;
            }
            else if (AggregateFrequency.Value > 0 && pausedDueToZeroFrequency)
            {
                Debug.Assert(channelState == PlaybackState.Paused, $"channel not paused while {nameof(pausedDueToZeroFrequency)} set.");

                pausedDueToZeroFrequency = false;
                Bass.ChannelPlay(channel);
            }
        }

        private double bassFreq => MathHelper.Clamp(initialFrequency * AggregateFrequency.Value, 100, 100000);

        public override bool Looping
        {
            get => base.Looping;
            set
            {
                base.Looping = value;
                setLoopFlag(Looping);
            }
        }

        public override void Play(bool restart = true)
        {
            base.Play(restart);

            EnqueueAction(() =>
            {
                if (!IsLoaded)
                {
                    channel = 0;
                    return;
                }

                // Remove looping flag from previous channel to prevent endless playback
                setLoopFlag(false);

                // We are creating a new channel for every playback, since old channels may
                // be overridden when too many other channels are created from the same sample.
                channel = ((SampleBass)Sample).CreateChannel();

                Bass.ChannelSetAttribute(channel, ChannelAttribute.NoRamp, 1);
                Bass.ChannelGetAttribute(channel, ChannelAttribute.Frequency, out initialFrequency);
                setLoopFlag(Looping);

                bassAmplitudeProcessor?.SetChannel(channel);
            });

            InvalidateState();

            EnqueueAction(() =>
            {
                if (channel != 0 && !pausedDueToZeroFrequency)
                    Bass.ChannelPlay(channel, restart);
            });

            // Needs to happen on the main thread such that
            // Played does not become true for a short moment.
            playing = true;
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
