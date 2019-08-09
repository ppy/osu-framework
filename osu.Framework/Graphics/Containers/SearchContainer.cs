// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Caching;
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
            }
        }

        private void performFilter()
        {
            var terms = (searchTerm ?? string.Empty).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            Children.OfType<IFilterable>().ForEach(child => match(child, terms, terms.Length > 0));
        }

        private static bool match(IFilterable filterable, IEnumerable<string> terms, bool searchActive)
        {
            //Words matched by parent is not needed to match children
            var childTerms = terms.Where(term =>
                !filterable.FilterTerms.Any(filterTerm =>
                    filterTerm.IndexOf(term, StringComparison.InvariantCultureIgnoreCase) >= 0)).ToArray();

            var hasFilterableChildren = filterable as IHasFilterableChildren;

            bool matching = childTerms.Length == 0;

            //We need to check the children and should any child match this matches as well
            if (hasFilterableChildren != null)
                foreach (IFilterable child in hasFilterableChildren.FilterableChildren)
                    matching |= match(child, childTerms, searchActive);

            filterable.FilteringActive = searchActive;
            return filterable.MatchingFilter = matching;
        }
    }
}
