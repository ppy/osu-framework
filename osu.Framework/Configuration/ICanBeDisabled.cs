// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;

namespace osu.Framework.Configuration
{
    /// <summary>
    /// Interface for objects that have a disabled state.
    /// </summary>
    public interface ICanBeDisabled
    {
        /// <summary>
        /// An event which is raised when <see cref="Disabled"/>'s state has changed.
        /// </summary>
        event Action<bool> DisabledChanged;

        /// <summary>
        /// Whether this object has been disabled.
        /// </summary>
        bool Disabled { get; }
    }
}
