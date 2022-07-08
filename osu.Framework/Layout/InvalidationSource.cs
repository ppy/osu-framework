// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using osu.Framework.Graphics;

namespace osu.Framework.Layout
{
    /// <summary>
    /// Indicates the source of an invalidation of a <see cref="Drawable"/>.
    /// </summary>
    [Flags]
    public enum InvalidationSource
    {
        /// <summary>
        /// The invalidation originates from the current <see cref="Drawable"/>.
        /// </summary>
        Self = 1,

        /// <summary>
        /// The invalidation originates from a parent in the supertree of the current <see cref="Drawable"/>.
        /// </summary>
        Parent = 2,

        /// <summary>
        /// The invalidation originates from a child in the subtree of the current <see cref="Drawable"/>.
        /// </summary>
        Child = 4,

        /// <summary>
        /// The default invalidation source. Deconstructs into <see cref="Self"/> <code>or</code> <see cref="Parent"/>.
        /// </summary>
        Default = Self | Parent,
    }
}
