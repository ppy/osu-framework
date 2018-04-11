// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Extensions.IEnumerableExtensions;

namespace osu.Framework.Graphics.Containers
{
    public class SearchContainer : SearchContainer<Drawable>
    {
    }

    public class SearchContainer<T> : FillFlowContainer<T> where T : Drawable
    {
        private string searchTerm;

        /// <summary>
        /// A string that should match the <see cref="IFilterable"/> children
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
                var terms = value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                Children.OfType<IFilterable>().ForEach(child => match(child, terms));
            }
        }

        private static bool match(IFilterable filterable, IEnumerable<string> terms)
        {
            //Words matched by parent is not needed to match children
            var childTerms = terms.Where(term =>
                !filterable.FilterTerms.Any(filterTerm =>
                    filterTerm.IndexOf(term, StringComparison.InvariantCultureIgnoreCase) >= 0)).ToArray();

            var hasFilterableChildren = filterable as IHasFilterableChildren;

            bool matching = childTerms.Length == 0;

            //We need to check the children and should any child match this matches aswell
            if (hasFilterableChildren != null)
                foreach (IFilterable searchableChildren in hasFilterableChildren.FilterableChildren)
                    matching |= match(searchableChildren, childTerms);

            return filterable.MatchingFilter = matching;
        }
    }
}
