// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Graphics.Performance
{
    /// <summary>
    /// Represents one of boundaries of lifetime interval.
    /// </summary>
    public enum LifetimeBoundaryKind
    {
        /// <summary>
        /// <see cref="Drawable.LifetimeStart"/>.
        /// </summary>
        Start,

        /// <summary>
        /// <see cref="Drawable.LifetimeEnd"/>.
        /// </summary>
        End,
    }
}
