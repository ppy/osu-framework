// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using ManagedBass;

namespace osu.Framework.Audio.Track
{
    /// <summary>
    /// Computes and caches amplitudes for a bass channel.
    /// </summary>
    public class BassAmplitudes
    {
        private int channel;

        private TrackAmplitudes currentAmplitudes;

        public TrackAmplitudes CurrentAmplitudes => currentAmplitudes;

        private const int size = 256; // should be half of the FFT length provided to ChannelGetData.

        private static readonly TrackAmplitudes empty = new TrackAmplitudes { FrequencyAmplitudes = new float[size] };

        public BassAmplitudes(int channel)
        {
            this.channel = channel;

            setEmpty();
        }

        public void SetChannel(int channel)
        {
            if (this.channel != 0)
                // just for simple thread safety. limitation can be easily removed later if required.
                throw new InvalidOperationException("Can only set channel to non-zero value once");

            if (channel == 0)
                throw new ArgumentException("Channel must be non-zero", nameof(channel));

            this.channel = channel;
        }

        public void Update()
        {
            if (channel == 0)
                return;

            bool active = Bass.ChannelIsActive(channel) == PlaybackState.Playing;

            var leftChannel = active ? Bass.ChannelGetLevelLeft(channel) / 32768f : -1;
            var rightChannel = active ? Bass.ChannelGetLevelRight(channel) / 32768f : -1;

            if (leftChannel >= 0 && rightChannel >= 0)
            {
                currentAmplitudes.LeftChannel = leftChannel;
                currentAmplitudes.RightChannel = rightChannel;

                float[] tempFrequencyData = new float[size];
                Bass.ChannelGetData(channel, tempFrequencyData, (int)DataFlags.FFT512);
                currentAmplitudes.FrequencyAmplitudes = tempFrequencyData;
            }
            else
                setEmpty();
        }

        private void setEmpty()
        {
            currentAmplitudes.LeftChannel = 0;
            currentAmplitudes.RightChannel = 0;
            currentAmplitudes.FrequencyAmplitudes = empty.FrequencyAmplitudes;
        }
    }
}
