// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using ManagedBass;

namespace osu.Framework.Audio.Sample
{
    internal class SampleChannelBass : SampleChannel, IBassAudio
    {
        private volatile int channel;
        private volatile bool playing;

        public override bool IsLoaded => Sample.IsLoaded;

        private float initialFrequency;

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

        internal override void OnStateChanged()
        {
            base.OnStateChanged();

            if (channel != 0)
            {
                Bass.ChannelSetAttribute(channel, ChannelAttribute.Volume, VolumeCalculated);
                Bass.ChannelSetAttribute(channel, ChannelAttribute.Pan, BalanceCalculated);
                Bass.ChannelSetAttribute(channel, ChannelAttribute.Frequency, initialFrequency * FrequencyCalculated);
            }
        }

        public override void Play(bool restart = true)
        {
            EnqueueAction(() =>
            {
                if (!IsLoaded)
                {
                    channel = 0;
                    return;
                }

                // We are creating a new channel for every playback, since old channels may
                // be overridden when too many other channels are created from the same sample.
                channel = ((SampleBass)Sample).CreateChannel();
                Bass.ChannelGetAttribute(channel, ChannelAttribute.Frequency, out initialFrequency);
            });

            InvalidateState();

            EnqueueAction(() =>
            {
                if (channel != 0)
                    Bass.ChannelPlay(channel, restart);
            });

            // Needs to happen on the main thread such that
            // Played does not become true for a short moment.
            playing = true;

            base.Play(restart);
        }

        protected override void UpdateState()
        {
            base.UpdateState();
            playing = channel != 0 && Bass.ChannelIsActive(channel) != 0;
        }

        public override void Stop()
        {
            if (channel == 0) return;

            base.Stop();

            EnqueueAction(() =>
            {
                Bass.ChannelStop(channel);
                // ChannelStop frees the channel.
                channel = 0;
            });
        }

        public override bool Playing => playing;
    }
}
