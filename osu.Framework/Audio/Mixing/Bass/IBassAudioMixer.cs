// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable enable

namespace osu.Framework.Audio.Mixing.Bass
{
    /// <summary>
    /// Interface for a BASS audio mixer, providing redirects for common BASS methods.
    /// </summary>
    internal interface IBassAudioMixer : IAudioMixer
    {
        /// <summary>
        /// Signals that a <see cref="IBassAudioChannel"/>'s handle should be added to the mix.
        /// </summary>
        /// <param name="channel">The <see cref="IBassAudioChannel"/> providing the handle.</param>
        void RegisterHandle(IBassAudioChannel channel);

        /// <summary>
        /// Signals that a <see cref="IBassAudioChannel"/>'s handle should be removed to the mix.
        /// </summary>
        /// <param name="channel">The <see cref="IBassAudioChannel"/> to free.</param>
        void UnregisterHandle(IBassAudioChannel channel);
    }
}
