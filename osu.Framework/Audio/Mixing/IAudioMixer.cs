// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

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
        /// Creates a new instance of <see cref="AudioEffect"/>.
        /// </summary>
        /// <param name="priority">Effect priority</param>
        /// <returns>A new <see cref="AudioEffect"/></returns>
        AudioEffect GetNewEffect(int priority = 0);
    }
}
