// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using ManagedBass;
using osu.Framework.Bindables;

#nullable enable

namespace osu.Framework.Audio.Mixing
{
    /// <summary>
    /// Interface for an audio mixer.
    /// </summary>
    public interface IAudioMixer
    {
        /// <summary>
        /// The effects currently applied to the mix.
        /// </summary>
        BindableList<IEffectParameter> Effects { get; }

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
    }
}
