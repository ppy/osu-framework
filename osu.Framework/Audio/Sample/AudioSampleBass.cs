// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using ManagedBass;

namespace osu.Framework.Audio.Sample
{
    class AudioSampleBass : AudioSample
    {
        private int channel;

        bool hasChannel => channel != 0;
        bool hasSample => SampleId != 0;

        public int SampleId { get; private set; }

        float initialFrequency;

        private bool freeWhenDone;

        public AudioSampleBass(byte[] data)
            : this(Bass.SampleLoad(data, 0, data.Length, 8, BassFlags.Default))
        {
        }

        public AudioSampleBass(int sampleId, bool freeWhenDone = false)
        {
            SampleId = sampleId;
            this.freeWhenDone = freeWhenDone;
        }

        private int ensureChannel()
        {
            if (!hasSample) return 0;

            if (!hasChannel)
            {
                channel = Bass.SampleGetChannel(SampleId);
                Bass.ChannelGetAttribute(channel, ChannelAttribute.Frequency, out initialFrequency);
                InvalidateState();
            }

            return channel;
        }

        void resetChannel()
        {
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
            if (!hasSample)
                return;

            base.Play();

            Bass.ChannelPlay(ensureChannel(), restart);
        }

        public override void Stop()
        {
            if (!hasChannel) return;

            base.Stop();

            Bass.ChannelStop(channel);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (freeWhenDone)
            {
                Bass.SampleFree(SampleId);
                SampleId = 0;
            }
        }

        public override void Pause()
        {
            if (!hasChannel) return;

            base.Pause();
            Bass.ChannelPause(channel);
        }

        public override bool Playing => hasChannel && Bass.ChannelIsActive(channel) != 0;
    }
}
