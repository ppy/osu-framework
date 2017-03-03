using osu.Framework.Allocation;
using osu.Framework.Lists;
using osu.Framework.Threading;
using System;
using System.Collections.Generic;

namespace osu.Framework.Graphics.Containers
{
    public class SearchContainer : SearchContainer<Drawable>
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
                    match(Children);
                    AfterMatching();
                });
            }
        }

        public int RematchDelay { get; set; } = 200;

        public delegate void OnSearchHandler(Drawable searchable);

        public OnSearchHandler OnMatch { get; set; } = delegate { };
        public OnSearchHandler OnMismatch { get; set; } = delegate { };
        public Action AfterMatching { get; set; } = delegate { };

        protected virtual bool AreMatching(string value1, string value2) => value1?.IndexOf(value2, StringComparison.InvariantCultureIgnoreCase) >= 0;

        private ScheduledDelegate rematch;

        private void match(IEnumerable<Drawable> children)
        {
            foreach (Drawable search in children)
            {
                if (search is ISearchable)
                {
                    bool contains = false;

                    foreach (string keyword in (search as ISearchable).Keywords)
                        if (AreMatching(keyword, Filter))
                            contains = true;

                    if (contains)
                        OnMatch(search);
                    else
                        OnMismatch(search);
                }
                else if(search is ISearchableChildren)
                {
                    match((search as ISearchableChildren).SearchableChildren);
                }
            }
        }
    }
}
