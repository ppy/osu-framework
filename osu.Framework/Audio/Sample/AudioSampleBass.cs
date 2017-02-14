// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using ManagedBass;

namespace osu.Framework.Audio.Sample
{
    class AudioSampleBass : AudioSample, IBassAudio
    {
        private volatile int channel;

        bool hasChannel => channel != 0;
        bool hasSample => SampleId != 0;

        public int SampleId { get; private set; }

        float initialFrequency;

        private bool freeWhenDone;

        public AudioSampleBass(byte[] data)
        {
            PendingActions.Enqueue(() => {
                SampleId = Bass.SampleLoad(data, 0, data.Length, 8, BassFlags.Default);
            });
        }

        public AudioSampleBass(int sampleId, bool freeWhenDone = false)
        {
            SampleId = sampleId;
            this.freeWhenDone = freeWhenDone;
        }

        void IBassAudio.UpdateDevice(int deviceIndex)
        {
            if (hasSample)
                // counter-intuitively, this is the correct API to use to migrate a sample to a new device.
                Bass.ChannelSetDevice(SampleId, deviceIndex);

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
                    channel = Bass.SampleGetChannel(SampleId);
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

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (freeWhenDone)
            {
                var s = SampleId;
                PendingActions.Enqueue(() =>
                {
                    Bass.SampleFree(s);
                });
                SampleId = 0;
            }
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
