// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

namespace osu.Framework.Configuration
{
    /// <summary>
    /// Interface for objects that have a description.
    /// </summary>
    public interface IHasDescription
    {
        /// <summary>
        /// The description for this object.
        /// </summary>
        string Description { get; }
    }
}
