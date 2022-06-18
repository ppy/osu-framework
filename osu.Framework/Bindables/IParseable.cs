// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

namespace osu.Framework.Bindables
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
