// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// Represents a component which can be filtered out on non-matching search terms.
    /// </summary>
    /// <remarks>
    /// Hiding <see cref="IFilterable"/>s for purposes other than filtering require being wrapped in a
    /// <see cref="VisibilityContainer"/> with its visibility state set to <see cref="Visibility.Hidden"/>.
    /// </remarks>
    public interface IFilterable : IHasFilterTerms
    {
        /// <summary>
        /// Whether the current object is matching (ie. visible) given the current filter criteria of a parent.
        /// </summary>
        bool MatchingFilter { set; }

        /// <summary>
        /// Whether a filter is currently being performed.
        /// </summary>
        bool FilteringActive { set; }
    }
}
