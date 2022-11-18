// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Audio.Track;

namespace osu.Framework.Audio
{
    public interface IHasAmplitudes
    {
        /// <summary>
        /// Current amplitude of stereo channels where 1 is full volume and 0 is silent.
        /// LeftChannel and RightChannel represent the maximum current amplitude of all of the left and right channels respectively.
        /// The most recent values are returned. Synchronisation between channels should not be expected.
        /// </summary>
        ChannelAmplitudes CurrentAmplitudes { get; }
    }
}
