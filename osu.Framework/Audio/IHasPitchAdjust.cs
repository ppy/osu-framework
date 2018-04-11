// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

namespace osu.Framework.Audio
{
    public interface IHasPitchAdjust
    {
        /// <summary>
        /// The pitch this track is playing at, relative to original.
        /// </summary>
        double PitchAdjust { get; set; }
    }
}
