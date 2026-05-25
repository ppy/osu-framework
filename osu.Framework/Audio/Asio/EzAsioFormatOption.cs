// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Audio.Asio
{
    /// <summary>
    /// A supported ASIO output format (sample rate + bit depth).
    /// </summary>
    public readonly struct EzAsioFormatOption : IEquatable<EzAsioFormatOption>
    {
        public static readonly int[] COMMON_SAMPLE_RATES = { 48000, 44100, 96000, 192000 };
        public static readonly int[] SUPPORTED_BIT_DEPTHS = { 16, 24 };

        public int SampleRate { get; }
        public int BitDepth { get; }

        public EzAsioFormatOption(int sampleRate, int bitDepth)
        {
            SampleRate = sampleRate;
            BitDepth = bitDepth;
        }

        public string DisplayName => $"{SampleRate} Hz, {BitDepth}-bit";

        public override string ToString() => DisplayName;

        public bool Equals(EzAsioFormatOption other) => SampleRate == other.SampleRate && BitDepth == other.BitDepth;

        public override bool Equals(object? obj) => obj is EzAsioFormatOption other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(SampleRate, BitDepth);

        public static bool operator ==(EzAsioFormatOption left, EzAsioFormatOption right) => left.Equals(right);

        public static bool operator !=(EzAsioFormatOption left, EzAsioFormatOption right) => !left.Equals(right);
    }
}
