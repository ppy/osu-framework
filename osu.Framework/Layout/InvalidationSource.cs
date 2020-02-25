// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics;

namespace osu.Framework.Layout
{
    [Flags]
    public enum InvalidationSource
    {
        /// <summary>
        /// The invalidation originates from the current <see cref="Drawable"/>.
        /// </summary>
        Self,

        /// <summary>
        /// The invalidation originates from a parent in the supertree of the current <see cref="Drawable"/>.
        /// </summary>
        Parent,

        /// <summary>
        /// The invalidation originates from a child in the subtree of the current <see cref="Drawable"/>.
        /// </summary>
        Child,

        /// <summary>
        /// The default invalidation source. Deconstructs into <see cref="Self"/> <code>or</code> <see cref="Parent"/>.
        /// </summary>
        Default = Self | Parent,
    }
}
