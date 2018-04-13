// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

namespace osu.Framework.Configuration
{
    /// <summary>
    /// Represents a class which can be parsed from an arbitrary object.
    /// </summary>
    public interface IParseable
    {
        /// <summary>
        /// Parse an input into this instance.
        /// </summary>
        /// <param name="input">The input which is to be parsed.</param>
        void Parse(object input);
    }
}
