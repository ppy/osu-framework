// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;

namespace osu.Framework
{
    /// <summary>
    /// An object which has a state and allows external consumers to change the current state.
    /// </summary>
    /// <typeparam name="T">Generally an Enum type local to the class implementing this interface.</typeparam>
    public interface IStateful<T>
        where T : struct, IComparable
    {
        /// <summary>
        /// The current state of this object.
        /// </summary>
        T State { get; set; }
    }
}
