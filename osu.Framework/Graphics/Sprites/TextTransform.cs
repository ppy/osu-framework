// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Graphics.Sprites
{
    /// <summary>
    /// Types of text transforms that can be applied to a <see cref="SpriteText"/> displayed text.
    /// </summary>
    public enum TextTransform
    {
        /// <summary>
        /// Text is displayed as is.
        /// </summary>
        None,

        /// <summary>
        /// Text is displayed in uppercase.
        /// </summary>
        Uppercase,

        /// <summary>
        /// Text is displayed in capitalized case aka title case.
        /// </summary>
        Capitalize,

        /// <summary>
        /// Text is displayed in lowercase.
        /// </summary>
        Lowercase
    }
}
