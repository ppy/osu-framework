// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;

namespace osu.Framework.Graphics.Containers
{
    public class SearchContainer : SearchContainer<Drawable>
    {
    }

    public class SearchContainer<T> : Container<T>, IFilterableChildren where T : Drawable
    {
        private string searchTerm;

        /// <summary>
        /// A string that should match the children
        /// </summary>
        public string SearchTerm
        {
            get
            {
                return searchTerm;
            }
            set
            {
                searchTerm = value;
                match(this, new string[0]);
            }
        }

        public IEnumerable<IFilterable> FilterableChildren => Children.OfType<IFilterable>();
        public string[] FilterTerms => null;
        public bool MatchingCurrentFilter { get; set; }

        private bool match(IFilterable filterable, IEnumerable<string> terms)
        {
            var childTerms = new List<string>(terms);
            childTerms.AddRange(filterable.FilterTerms ?? new string[0]);

            var hasFilterableChildren = filterable as IHasFilterableChildren;

            if (hasFilterableChildren != null)
            {
                //We need to check the children and should any child match this matches aswell
                bool matching = false;

                    matching = match(searchableChildren, childTerms) || matching;
                foreach (IFilterable searchableChildren in hasFilterableChildren.FilterableChildren)

                return hasFilterableChildren.MatchingCurrentFilter = matching;
            }
            else
            {
                return filterable.MatchingCurrentFilter = childTerms.Any(term => term.IndexOf(SearchTerm, StringComparison.InvariantCultureIgnoreCase) >= 0);
            }
        }
    }
}
