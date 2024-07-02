// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Audio.Mixing.SDL3
{
    /// <summary>
    /// Interface for audio channels that feed audio to <see cref="SDL3AudioMixer"/>.
    /// </summary>
    internal interface ISDL3AudioChannel : IAudioChannel
    {
        /// <summary>
        /// Returns remaining audio samples.
        /// </summary>
        /// <param name="data">Audio data needs to be put here. Length of this determines how much data needs to be filled.</param>
        /// <returns>Sample count</returns>
        int GetRemainingSamples(float[] data);

        /// <summary>
        /// Mixer won't call <see cref="GetRemainingSamples(float[])"/> if this returns false.
        /// </summary>
        bool Playing { get; }

        /// <summary>
        /// Mixer uses this as volume, Value should be within 0 and 1.
        /// </summary>
        (float left, float right) Volume { get; }
    }
}
