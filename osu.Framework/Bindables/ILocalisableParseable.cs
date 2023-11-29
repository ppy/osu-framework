// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Globalization;

namespace osu.Framework.Bindables
{
    /// <summary>
    /// Represents a class which can be parsed from an arbitrary object based on locale information.
    /// </summary>
    public interface ILocalisableParseable
    {
        /// <summary>
        /// Parse an input into this instance.
        /// </summary>
        /// <param name="input">The input which is to be parsed.</param>
        /// <param name="cultureInfo">The preferred culture formatting to be displayed.</param>
        void Parse(object input, CultureInfo cultureInfo);
    }
}
