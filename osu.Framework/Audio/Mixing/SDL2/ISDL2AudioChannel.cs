// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Audio.Mixing.SDL2
{
    /// <summary>
    /// Interface for audio channels that feed audio to <see cref="SDL2AudioMixer"/>.
    /// </summary>
    internal interface ISDL2AudioChannel : IAudioChannel
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
        float Volume { get; }

        /// <summary>
        /// Mixer uses this to adjust channel balance. Value should be within -1.0 and 1.0
        /// </summary>
        float Balance { get; }
    }
}
