// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;

namespace osu.Framework.Audio.Track
{
    /// <summary>
    /// A collection of information providing per-channel and per-frequency amplitudes of an audio channel.
    /// </summary>
    public readonly struct ChannelAmplitudes
    {
        /// <summary>
        /// The length of the <see cref="FrequencyAmplitudes"/> data.
        /// </summary>
        public const int AMPLITUDES_SIZE = 256;

        /// <summary>
        /// The amplitude of the left channel (0..1).
        /// </summary>
        public readonly float LeftChannel;

        /// <summary>
        /// The amplitude of the right channel (0..1).
        /// </summary>
        public readonly float RightChannel;

        /// <summary>
        /// The maximum amplitude of the left and right channels (0..1).
        /// </summary>
        public float Maximum => Math.Max(LeftChannel, RightChannel);

        /// <summary>
        /// The average amplitude of the left and right channels (0..1).
        /// </summary>
        public float Average => (LeftChannel + RightChannel) / 2;

        /// <summary>
        /// 256 length array of bins containing the average frequency of both channels at every ~78Hz step of the audible spectrum (0Hz - 20,000Hz).
        /// </summary>
        public readonly ReadOnlyMemory<float> FrequencyAmplitudes;

        private static readonly float[] empty_array = new float[AMPLITUDES_SIZE];

        public ChannelAmplitudes(float leftChannel = 0, float rightChannel = 0, float[] amplitudes = null)
        {
            LeftChannel = leftChannel;
            RightChannel = rightChannel;
            FrequencyAmplitudes = amplitudes ?? empty_array;
        }

        public static ChannelAmplitudes Empty { get; } = new ChannelAmplitudes(0);
    }
}
