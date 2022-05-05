// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using osu.Framework.Caching;

namespace osu.Framework.Graphics.Containers
{
    public class SearchContainer : SearchContainer<Drawable>
    {
    }

    /// <summary>
    /// A container which filters children based on a search term.
    /// Re-filtering will only be performed when the <see cref="SearchTerm"/> changes, or
    /// new items are added as direct children of this container.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SearchContainer<T> : FillFlowContainer<T> where T : Drawable
    {
        /// <summary>
        /// Fired whenever a filter operation completes.
        /// </summary>
        public event Action FilterCompleted;

        private bool allowNonContiguousMatching;

        /// <summary>
        /// Whether the matching algorithm should consider cases where other characters exist between consecutive characters in the search term.
        /// If <c>true</c>, searching for "BSI" will match "BeatmapSetInfo".
        /// </summary>
        public bool AllowNonContiguousMatching
        {
            get => allowNonContiguousMatching;
            set
            {
                if (value == allowNonContiguousMatching)
                    return;

                allowNonContiguousMatching = value;
                filterValid.Invalidate();
            }
        }

        private string searchTerm;

        /// <summary>
        /// A string that should match the <see cref="IFilterable"/> children
        /// </summary>
        public string SearchTerm
        {
            get => searchTerm;
            set
            {
                if (value == searchTerm)
                    return;

                searchTerm = value;
                filterValid.Invalidate();
            }
        }

        private readonly Cached filterValid = new Cached();

        protected override void InvalidateLayout()
        {
            base.InvalidateLayout();
            filterValid.Invalidate();
        }

        protected override void Update()
        {
            base.Update();

            if (!filterValid.IsValid)
            {
                performFilter();
                filterValid.Validate();
                FilterCompleted?.Invoke();
            }
        }

        private void performFilter()
        {
            string[] terms = (searchTerm ?? string.Empty).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            matchSubTree(this, terms, terms.Length > 0, allowNonContiguousMatching);
        }

        /// <summary>
        /// Performs a tree traversal from the given node to filter out drawables which do not match the specified search terms.
        /// </summary>
        /// <param name="drawable">The root drawable of this subtree.</param>
        /// <param name="searchTerms">The search terms to filter drawables against.</param>
        /// <param name="searchActive">Whether there are search terms usable for filtering drawables.</param>
        /// <param name="nonContiguousMatching">Whether the matching algorithm should be non-contiguous, allowing for potentially non-matching characters to exist between matches.</param>
        private static bool matchSubTree(Drawable drawable, IReadOnlyList<string> searchTerms, bool searchActive, bool nonContiguousMatching)
        {
            bool matching = match(drawable, searchTerms, nonContiguousMatching, out var nonMatchingTerms);

            if (drawable is IContainerEnumerable<Drawable> container && (container as VisibilityContainer)?.State.Value != Visibility.Hidden)
            {
                foreach (var child in container.Children)
                    matching |= matchSubTree(child, nonMatchingTerms, searchActive, nonContiguousMatching);
            }

            if (drawable is IFilterable filterable)
            {
                filterable.FilteringActive = searchActive;
                filterable.MatchingFilter = matching;
            }

            return matching;
        }

        /// <summary>
        /// Whether the specified drawable matches the specified search terms.
        /// </summary>
        /// <param name="drawable">The drawable to check for match.</param>
        /// <param name="searchTerms">The search terms to check against.</param>
        /// <param name="nonContiguousMatching">Whether the matching algorithm should be non-contiguous, allowing for potentially non-matching characters to exist between matches.</param>
        /// <param name="nonMatchingTerms">The search terms which do not match the specified drawable.</param>
        private static bool match(Drawable drawable, IReadOnlyList<string> searchTerms, bool nonContiguousMatching, out IReadOnlyList<string> nonMatchingTerms)
        {
            nonMatchingTerms = searchTerms;

            if (drawable is IFilterable filterable)
            {
                nonMatchingTerms = nonMatchingTerms.Where(term => !checkTerms(filterable.FilterTerms, term, nonContiguousMatching)).ToArray();

                // reset filter to ensure presence is not influenced by previous filter state when checking.
                filterable.MatchingFilter = true;
            }

            return nonMatchingTerms.Count == 0;
        }

        private static bool checkTerms(IEnumerable<string> filterTerms, string searchTerm, bool nonContiguous) => filterTerms.Any(term => checkTerm(term, searchTerm, nonContiguous));

        /// <summary>
        /// Check whether a search term exists in a forward direction, allowing for potentially non-matching characters to exist between matches.
        /// </summary>
        private static bool checkTerm(string haystack, string needle, bool nonContiguous)
        {
            if (!nonContiguous)
                return haystack.Contains(needle, StringComparison.OrdinalIgnoreCase);

            int index = 0;

            for (int i = 0; i < needle.Length; i++)
            {
                // string.IndexOf doesn't have an overload which takes both a `startIndex` and `StringComparison` mode.
                int found = CultureInfo.InvariantCulture.CompareInfo.IndexOf(haystack, needle[i], index, CompareOptions.OrdinalIgnoreCase);
                if (found < 0)
                    return false;

                index = found + 1;
            }

            return true;
        }
    }
}
