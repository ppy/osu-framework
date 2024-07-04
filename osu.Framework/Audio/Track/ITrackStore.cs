// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Audio.Track
{
    public interface ITrackStore : IAdjustableResourceStore<Track>
    {
        /// <summary>
        /// Retrieve a <see cref="TrackVirtual"/> with no audio device backing.
        /// </summary>
        /// <param name="length">The length of the virtual track.</param>
        /// <param name="name">A name to identify the virtual track internally.</param>
        /// <returns>A new virtual track.</returns>
        Track GetVirtual(double length = double.PositiveInfinity, string name = "virtual");
    }
}
