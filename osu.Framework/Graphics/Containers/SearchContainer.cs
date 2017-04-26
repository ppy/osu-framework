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
        /// String to filter <see cref="IFilterable"/>
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
        public string[] Terms => null;
        public bool FilteredByParent { get; set; }

        private bool match(IFilterable searchable, IEnumerable<string> terms)
        {
            var childTerms = new List<string>(terms);
            childTerms.AddRange(searchable.Terms ?? new string[0]);

            var searchContainer = searchable as IFilterableChildren;

            if (searchContainer != null)
            {
                bool matching = false;

                foreach (IFilterable searchableChildren in searchContainer.FilterableChildren)
                    matching = match(searchableChildren, childTerms) || matching;

                return searchContainer.FilteredByParent = matching;
            }
            else
            {
                return searchable.FilteredByParent = childTerms.Any(term => term.IndexOf(SearchTerm, StringComparison.InvariantCultureIgnoreCase) >= 0);
            }
        }
    }
}
