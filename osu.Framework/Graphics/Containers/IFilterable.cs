// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Graphics.Containers
{
    public interface IFilterable : IHasFilterTerms
    {
        /// <summary>
        /// Whether the current object is matching (ie. visible) given the current filter criteria of a parent.
        /// </summary>
        /// <remarks>
        /// It is recommended to not let this property update the <see cref="Drawable.Alpha"/> state of this filterable, as to not conflict with hiding it via other means.
        /// But instead, override <see cref="Drawable.IsPresent"/> to respect the filter state, and invalidate <see cref="Invalidation.Presence"/> accordingly.
        /// </remarks>
        bool MatchingFilter { set; }

        /// <summary>
        /// Whether a filter is currently being performed.
        /// </summary>
        bool FilteringActive { set; }
    }
}
