// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using ManagedBass;

#nullable enable

namespace osu.Framework.Audio.Mixing
{
    /// <summary>
    /// Interface for an audio mixer.
    /// </summary>
    public interface IAudioMixer
    {
        /// <summary>
        /// Adds an <see cref="IAudioChannel"/> to the mix.
        /// </summary>
        /// <param name="channel">The <see cref="IAudioChannel"/> to add.</param>
        void Add(IAudioChannel channel);

        /// <summary>
        /// Removes an <see cref="IAudioChannel"/> from the mix.
        /// </summary>
        /// <param name="channel">The <see cref="IAudioChannel"/> to remove.</param>
        void Remove(IAudioChannel channel);

        /// <summary>
        /// Applies an effect to the mix.
        /// </summary>
        /// <param name="effect">The effect to apply.</param>
        /// <param name="priority">The effect priority.</param>
        void ApplyEffect(IEffectParameter effect, int priority);

        /// <summary>
        /// Removes an effect from the mix.
        /// </summary>
        /// <param name="effect">The effect to remove.</param>
        void RemoveEffect(IEffectParameter effect);
    }
}
