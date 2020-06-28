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

        public ChannelAmplitudes CurrentAmplitudes { get; private set; } = ChannelAmplitudes.Empty;

        public BassAmplitudeProcessor(int channel)
        {
            this.channel = channel;
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
                float[] frequencyData = new float[ChannelAmplitudes.AMPLITUDES_SIZE];
                Bass.ChannelGetData(ch, frequencyData, (int)DataFlags.FFT512);
                CurrentAmplitudes = new ChannelAmplitudes(leftChannel, rightChannel, frequencyData);
            }
            else
                CurrentAmplitudes = ChannelAmplitudes.Empty;
        }
    }
}
