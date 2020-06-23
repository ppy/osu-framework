// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using ManagedBass;
using osu.Framework.Audio.Track;

namespace osu.Framework.Audio
{
    /// <summary>
    /// Computes and caches amplitudes for a bass channel.
    /// </summary>
    public class BassAmplitudeProcessor
    {
        private int channel;

        private ChannelAmplitudes currentAmplitudes;

        public ChannelAmplitudes CurrentAmplitudes => currentAmplitudes;

        private const int size = 256; // should be half of the FFT length provided to ChannelGetData.

        private static readonly ChannelAmplitudes empty = new ChannelAmplitudes { FrequencyAmplitudes = new float[size] };

        public BassAmplitudeProcessor(int channel)
        {
            this.channel = channel;

            setEmpty();
        }

        public void SetChannel(int channel)
        {
            this.channel = channel;
        }

        public void Update()
        {
            int ch = channel;

            if (ch == 0)
                return;

            bool active = Bass.ChannelIsActive(ch) == PlaybackState.Playing;

            var leftChannel = active ? Bass.ChannelGetLevelLeft(ch) / 32768f : -1;
            var rightChannel = active ? Bass.ChannelGetLevelRight(ch) / 32768f : -1;

            if (leftChannel >= 0 && rightChannel >= 0)
            {
                currentAmplitudes.LeftChannel = leftChannel;
                currentAmplitudes.RightChannel = rightChannel;

                float[] tempFrequencyData = new float[size];
                Bass.ChannelGetData(ch, tempFrequencyData, (int)DataFlags.FFT512);
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
