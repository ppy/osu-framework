// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Threading;
using System;
using System.Collections.Generic;

namespace osu.Framework.Graphics.Containers
{
    public class SearchContainer : SearchContainer<Drawable>, ISearchableChildren
    { }

    public class SearchContainer<T> : Container<T> where T : Drawable
    {
        private string filter;
        public string Filter
        {
            get
            {
                return filter;
            }
            set
            {
                filter = value;
                rematch?.Cancel();
                Delay(RematchDelay);
                rematch = Schedule(delegate
                {
                    match(new[] {this}, new List<string>());
                });
            }
        }

        public IEnumerable<Drawable> SearchableContent { get; set; } = null;

        public string[] Keywords => null;
        public IEnumerable<Drawable> SearchableChildren => SearchableContent ?? Children;
        public Action AfterSearch { get; set; }

        public int RematchDelay { get; set; } = 200;

        public delegate void OnSearchHandler(Drawable searchable);

        public OnSearchHandler OnMatch { get; set; } = delegate { };
        public OnSearchHandler OnMismatch { get; set; } = delegate { };

        protected virtual bool AreMatching(string value1, string value2) => value1?.IndexOf(value2, StringComparison.InvariantCultureIgnoreCase) >= 0;

        private ScheduledDelegate rematch;

        private void match(IEnumerable<Drawable> children, List<string> parentKeywords)
        {
            foreach (Drawable drawable in children)
            {
                var search = drawable as ISearchable;
                if (search == null)
                    continue;

                var searchableContainer = search as ISearchableChildren;
                if(searchableContainer != null)
                {
                    if (searchableContainer.Keywords != null && searchableContainer.Keywords.Length != 0)
                    {
                        var childrenKeywords = new List<string>(parentKeywords);
                        childrenKeywords.AddRange(searchableContainer.Keywords);
                        match(searchableContainer.SearchableChildren, childrenKeywords);
                    }
                    else
                        match(searchableContainer.SearchableChildren, parentKeywords);
                    searchableContainer.AfterSearch?.Invoke();
                }
                else if (search.Keywords != null && search.Keywords.Length != 0)
                {
                    bool contains = false;

                    foreach (string keyword in search.Keywords)
                        if (AreMatching(keyword, Filter))
                            contains = true;

                    foreach (string keyword in parentKeywords)
                        if (AreMatching(keyword, Filter))
                            contains = true;

                    if (contains)
                        OnMatch(drawable);
                    else
                        OnMismatch(drawable);
                }
            }
        }
    }
}
