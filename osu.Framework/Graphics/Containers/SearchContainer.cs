// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;

namespace osu.Framework.Graphics.Containers
{
    public class SearchContainer : SearchContainer<Drawable>
    { }

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
                match(this);
            }
        }

        public IEnumerable<IFilterable> FilterableChildren => Children.OfType<IFilterable>();
        public string[] Keywords => null;
        public bool FilteredByParent { get; set; }


        private bool match(IFilterable searchable, List<string> parentKeywords = null)
        {
            var searchContainer = searchable as IFilterableChildren;

            var keywords = new List<string>(searchable.Keywords ?? new string[0]);
            keywords.AddRange(parentKeywords ?? new string[0].ToList());

            if (searchContainer != null)
            {
                bool matching = false;
                foreach(IFilterable searchableChildren in searchContainer.FilterableChildren)
                    matching = match(searchableChildren, keywords) || matching;
                return searchContainer.FilteredByParent = matching;
            }
            else
            {
                return searchable.FilteredByParent = keywords.Any(keyword => keyword.IndexOf(SearchTerm, StringComparison.InvariantCultureIgnoreCase) >= 0);
            }
        }
    }
}
