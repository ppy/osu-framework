// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

namespace osu.Framework.Configuration
{
    /// <summary>
    /// Interface for objects that have a default value.
    /// </summary>
    public interface IHasDefaultValue
    {
        /// <summary>
        /// Check whether this object has its default value.
        /// </summary>
        bool IsDefault { get; }
    }
}
