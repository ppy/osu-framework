// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;

namespace osu.Framework
{
    /// <summary>
    /// An object which has a state and allows external consumers to change the current state.
    /// </summary>
    /// <typeparam name="T">Generally an Enum type local to the class implementing this interface.</typeparam>
    public interface IStateful<T>
        where T : struct
    {
        /// <summary>
        /// Invoked when the state of this <see cref="IStateful{T}"/> has changed.
        /// </summary>
        event Action<T> StateChanged;

        /// <summary>
        /// The current state of this object.
        /// </summary>
        T State { get; set; }
    }
}
