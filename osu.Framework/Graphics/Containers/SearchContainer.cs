// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Caching;
using osu.Framework.Localisation;
using osu.Framework.Extensions.IEnumerableExtensions;

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

        [Resolved]
        private LocalisationManager localisation { get; set; }

        protected internal override void AddInternal(Drawable drawable)
        {
            base.AddInternal(drawable);
            filterValid.Invalidate();
        }

        private readonly Cached filterValid = new Cached();

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
            Children.OfType<IFilterable>().ForEach(child => match(child, terms, terms.Length > 0, allowNonContiguousMatching));
        }

        private bool match(IFilterable filterable, IEnumerable<string> searchTerms, bool searchActive, bool nonContiguousMatching)
        {
            IEnumerable<string> filterTerms = filterable.FilterTerms.SelectMany(localisedStr =>
                new[] { localisedStr.ToString(), localisation.GetLocalisedString(localisedStr) });

            //Words matched by parent is not needed to match children
            string[] childTerms = searchTerms.Where(term =>
                !filterTerms.Any(filterTerm =>
                    checkTerm(filterTerm, term, nonContiguousMatching))).ToArray();

            bool matching = childTerms.Length == 0;

            //We need to check the children and should any child match this matches as well
            if (filterable is IHasFilterableChildren hasFilterableChildren)
            {
                foreach (IFilterable child in hasFilterableChildren.FilterableChildren)
                    matching |= match(child, childTerms, searchActive, nonContiguousMatching);
            }

            filterable.FilteringActive = searchActive;
            return filterable.MatchingFilter = matching;
        }

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
