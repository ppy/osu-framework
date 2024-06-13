// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using ManagedBass;
using ManagedBass.Fx;

namespace osu.Framework.Audio.Mixing
{
    /// <summary>
    /// An audio mixer which one or more <see cref="IAudioChannel"/>s can be routed into.
    /// Supports DSP effects independent of other <see cref="IAudioMixer"/>s.
    /// </summary>
    public interface IAudioMixer
    {
        /// <summary>
        /// Adds a channel to the mix.
        /// </summary>
        /// <param name="channel">The channel to add.</param>
        void Add(IAudioChannel channel);

        /// <summary>
        /// Removes a channel from the mix.
        /// </summary>
        /// <param name="channel">The channel to remove.</param>
        void Remove(IAudioChannel channel);

        /// <summary>
        /// Applies an effect to the mixer.
        /// </summary>
        /// <param name="effect">The effect (e.g. <see cref="BQFEffect"/>.</param>
        /// <param name="priority">The effect priority. Lower values indicate higher priority and negative values are allowed.
        ///     When there are multiple effects with the same priority, their ordering depends on the order in which they are added to the <see cref="IAudioMixer"/>.</param>
        void AddEffect(IEffectParameter effect, int priority = 0);

        /// <summary>
        /// Removes an effect from the mixer.
        /// </summary>
        /// <param name="effect">The effect.</param>
        void RemoveEffect(IEffectParameter effect);

        /// <summary>
        /// Updates an effect's parameters.
        /// </summary>
        /// <param name="effect"></param>
        void UpdateEffect(IEffectParameter effect);
    }
}
