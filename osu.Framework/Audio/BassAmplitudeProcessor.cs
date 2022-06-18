// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using ManagedBass;
using osu.Framework.Audio.Mixing.Bass;
using osu.Framework.Audio.Track;

namespace osu.Framework.Audio
{
    /// <summary>
    /// Computes and caches amplitudes for a bass channel.
    /// </summary>
    internal class BassAmplitudeProcessor
    {
        /// <summary>
        /// The most recent amplitude data. Note that this is updated on an ongoing basis and there is no guarantee it is in a consistent (single sample) state.
        /// If you need consistent data, make a copy of FrequencyAmplitudes while on the audio thread.
        /// </summary>
        public ChannelAmplitudes CurrentAmplitudes { get; private set; } = ChannelAmplitudes.Empty;

        private readonly IBassAudioChannel channel;

        public BassAmplitudeProcessor(IBassAudioChannel channel)
        {
            this.channel = channel;
        }

        private float[]? frequencyData;

        private readonly float[] channelLevels = new float[2];

        public void Update()
        {
            if (channel.Handle == 0)
                return;

            bool active = channel.Mixer.ChannelIsActive(channel) == PlaybackState.Playing;

            channel.Mixer.ChannelGetLevel(channel, channelLevels, 1 / 60f, LevelRetrievalFlags.Stereo);

            float leftChannel = active ? channelLevels[0] : -1;
            float rightChannel = active ? channelLevels[1] : -1;

            if (leftChannel >= 0 && rightChannel >= 0)
            {
                frequencyData ??= new float[ChannelAmplitudes.AMPLITUDES_SIZE];
                channel.Mixer.ChannelGetData(channel, frequencyData, (int)DataFlags.FFT512);
                CurrentAmplitudes = new ChannelAmplitudes(leftChannel, rightChannel, frequencyData);
            }
            else
                CurrentAmplitudes = ChannelAmplitudes.Empty;
        }
    }
}
