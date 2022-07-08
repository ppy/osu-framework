// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using ManagedBass;
using osu.Framework.Bindables;

namespace osu.Framework.Audio.Mixing
{
    /// <summary>
    /// An audio mixer which one or more <see cref="IAudioChannel"/>s can be routed into.
    /// Supports DSP effects independent of other <see cref="IAudioMixer"/>s.
    /// </summary>
    public interface IAudioMixer
    {
        /// <summary>
        /// The effects currently applied to the mix.
        /// <para>
        /// Effects are stored in order of decreasing priority such that the effect at <c>index = 0</c> in the list has the highest priority
        /// and the effect at <c>index = Count - 1</c> in the list has the lowest priority.
        /// </para>
        /// </summary>
        BindableList<IEffectParameter> Effects { get; }

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
    }
}
