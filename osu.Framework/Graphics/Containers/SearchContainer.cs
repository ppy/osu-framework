// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;

namespace osu.Framework.Graphics.Containers
{
    public class SearchContainer : SearchContainer<Drawable>
    { }

    public class SearchContainer<T> : Container<T>, ISearchableChildren where T : Drawable
    {
        private string searchTerm;
        /// <summary>
        /// String to match with the <see cref="ISearchable"/>s
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

        public IEnumerable<ISearchable> SearchableChildren => Children.OfType<ISearchable>();
        public string[] Keywords => null;
        public bool Matching { get; set; }


        private bool match(ISearchable searchable, List<string> parentKeywords = null)
        {
            var searchContainer = searchable as ISearchableChildren;
            
            var keywords = new List<string>(searchable.Keywords ?? new string[0]);
            keywords.AddRange(parentKeywords ?? new string[0].ToList());

            if (searchContainer != null)
            {
                bool matching = false;
                foreach(ISearchable searchableChildren in searchContainer.SearchableChildren)
                    matching = match(searchableChildren, keywords) || matching;
                

                searchContainer.Matching = matching;
                return matching;
            }
            else
            {
                bool matching = keywords.Any(keyword => keyword.IndexOf(SearchTerm, StringComparison.InvariantCultureIgnoreCase) >= 0);

                searchable.Matching = matching;
                return matching;
            }
        }
    }
}
