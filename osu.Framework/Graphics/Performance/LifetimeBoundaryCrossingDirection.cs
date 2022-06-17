// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

namespace osu.Framework.Graphics.Performance
{
    /// <summary>
    /// Represents a direction of lifetime boundary crossing.
    /// </summary>
    public enum LifetimeBoundaryCrossingDirection
    {
        /// <summary>
        /// A crossing from past to future.
        /// </summary>
        Forward,

        /// <summary>
        /// A crossing from future to past.
        /// </summary>
        Backward,
    }
}
