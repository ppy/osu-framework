using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

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
                needsRematch = true;
            }
        }

        public StringComparison Comparator { get; set; } = StringComparison.OrdinalIgnoreCase;

        private bool needsRematch;

        public delegate void OnSearchHandler(Drawable searchable);
        
        public OnSearchHandler OnMatch { get; set; }
        public OnSearchHandler OnMismatch { get; set; }

        protected override void Update()
        {
            base.Update();

            if (needsRematch)
            {
                match(Children);
                needsRematch = false;
            }
        }

        private void match(IEnumerable<Drawable> children)
        {
            foreach (Drawable search in children)
            {
                if (!(search is ISearchable))
                {
                    if (search is IContainer)
                        match((search as IContainerEnumerable<Drawable>).Children);
                    continue;
                }

                bool contains = false;

                foreach (string keyword in (search as ISearchable).Keywords)
                    if (keyword.IndexOf(Filter, Comparator) >= 0)
                        contains = true;

                if (contains)
                    OnMatch(search);
                else
                    OnMismatch(search);
            }
        }
    }
}
