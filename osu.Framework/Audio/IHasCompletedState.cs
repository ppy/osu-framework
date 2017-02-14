// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

namespace osu.Framework.Audio
{
    public interface IHasCompletedState
    {
        /// <summary>
        /// Becomes true when we are out and done with this object (and pending clean-up).
        /// </summary>
        bool HasCompleted { get; }
    }
}
