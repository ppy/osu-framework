// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Graphics
{
    public enum BlendingEquation
    {
        /// <summary>
        /// Inherits from parent.
        /// </summary>
        Inherit = 0,

        /// <summary>
        /// Adds the source and destination colours.
        /// </summary>
        Add,

        /// <summary>
        /// Chooses the minimum of each component of the source and destination colours.
        /// </summary>
        Min,

        /// <summary>
        /// Chooses the maximum of each component of the source and destination colours.
        /// </summary>
        Max,

        /// <summary>
        /// Subtracts the destination colour from the source colour.
        /// </summary>
        Subtract,

        /// <summary>
        /// Subtracts the source colour from the destination colour.
        /// </summary>
        ReverseSubtract,
    }
}
