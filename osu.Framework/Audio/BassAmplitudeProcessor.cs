// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using ManagedBass;
using ManagedBass.Mix;
using osu.Framework.Audio.Track;

namespace osu.Framework.Audio
{
    /// <summary>
    /// Computes and caches amplitudes for a bass channel.
    /// </summary>
    public class BassAmplitudeProcessor
    {
        private int channel;

        /// <summary>
        /// The most recent amplitude data. Note that this is updated on an ongoing basis and there is no guarantee it is in a consistent (single sample) state.
        /// If you need consistent data, make a copy of FrequencyAmplitudes while on the audio thread.
        /// </summary>
        public ChannelAmplitudes CurrentAmplitudes { get; private set; } = ChannelAmplitudes.Empty;

        public BassAmplitudeProcessor(int channel)
        {
            this.channel = channel;
        }

        public void SetChannel(int channel)
        {
            this.channel = channel;
        }

        private float[] frequencyData;

        public void Update()
        {
            int ch = channel;

            if (ch == 0)
                return;

            bool active = Bass.ChannelIsActive(ch) == PlaybackState.Playing;

            float[] channelLevels = new float[2];
            BassMix.ChannelGetLevel(ch, channelLevels, 1 / 60f, LevelRetrievalFlags.Stereo);
            var leftChannel = active ? channelLevels[0] : -1;
            var rightChannel = active ? channelLevels[1] : -1;

            if (leftChannel >= 0 && rightChannel >= 0)
            {
                frequencyData ??= new float[ChannelAmplitudes.AMPLITUDES_SIZE];
                BassMix.ChannelGetData(ch, frequencyData, (int)DataFlags.FFT512);
                CurrentAmplitudes = new ChannelAmplitudes(leftChannel, rightChannel, frequencyData);
            }
            else
                CurrentAmplitudes = ChannelAmplitudes.Empty;
        }
    }
}
