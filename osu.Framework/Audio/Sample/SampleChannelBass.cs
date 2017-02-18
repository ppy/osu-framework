// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using ManagedBass;

namespace osu.Framework.Audio.Sample
{
    class SampleChannelBass : SampleChannel, IBassAudio
    {
        private volatile int channel;

        bool hasChannel => channel != 0;
        bool hasSample => Sample.IsLoaded;

        float initialFrequency;

        public SampleChannelBass(Sample sample) : base(sample)
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

        protected override void OnStateChanged(object sender, EventArgs e)
        {
            base.OnStateChanged(sender, e);

            if (hasChannel)
            {
                Bass.ChannelSetAttribute(channel, ChannelAttribute.Volume, VolumeCalculated);
                Bass.ChannelSetAttribute(channel, ChannelAttribute.Pan, BalanceCalculated);
                Bass.ChannelSetAttribute(channel, ChannelAttribute.Frequency, initialFrequency * FrequencyCalculated);
            }
        }

        public override void Play(bool restart = true)
        {
            base.Play();

            PendingActions.Enqueue(() =>
            {
                if (!hasSample)
                {
                    channel = 0;
                    return;
                }

                if (!hasChannel)
                {
                    channel = ((SampleBass)Sample).GetChannel();
                    Bass.ChannelGetAttribute(channel, ChannelAttribute.Frequency, out initialFrequency);
                }
            });

            InvalidateState();

            PendingActions.Enqueue(() =>
            {
                Bass.ChannelPlay(channel, restart);
            });
        }

        public override void Stop()
        {
            if (!hasChannel) return;

            base.Stop();

            PendingActions.Enqueue(() =>
            {
                Bass.ChannelStop(channel);
            });
        }

        public override void Pause()
        {
            if (!hasChannel) return;

            base.Pause();
            PendingActions.Enqueue(() =>
            {
                Bass.ChannelPause(channel);
            });
        }

        public override bool Playing => hasChannel && Bass.ChannelIsActive(channel) != 0; //consider moving this bass call to the update method.
    }
}
