// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Audio.Track
{
    public readonly struct ChannelAmplitudes
    {
        /// <summary>
        /// The length of the <see cref="FrequencyAmplitudes"/> data.
        /// </summary>
        public const int AMPLITUDES_SIZE = 256;

        public readonly float LeftChannel;
        public readonly float RightChannel;

        public float Maximum => Math.Max(LeftChannel, RightChannel);

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
