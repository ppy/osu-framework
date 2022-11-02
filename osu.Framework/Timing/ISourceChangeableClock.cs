// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Timing
{
    /// <summary>
    /// A clock which has a source that can be changed.
    /// </summary>
    public interface ISourceChangeableClock : IClock
    {
        /// <summary>
        /// The source clock.
        /// </summary>
        IClock? Source { get; }

        /// <summary>
        /// Change the source clock.
        /// </summary>
        /// <param name="source">The new source clock.</param>
        void ChangeSource(IClock? source);
    }
}
