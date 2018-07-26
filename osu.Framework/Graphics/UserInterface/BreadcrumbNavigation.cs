// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Graphics.Containers;

namespace osu.Framework.Graphics.UserInterface
{
    public class BreadcrumbNavigation : FillFlowContainer<Breadcrumb>
    {
        /// <summary>
        /// Override this method for customising the design of the breadcrumb.
        /// remember to set
        /// <code>
        ///    AutoSizeAxes = Axes.X,
        ///    RelativeSizeAxes = Axes.Y,
        /// </code>
        /// </summary>
        /// <param name="value">The text value that is supposed to be written in the breadcrumb</param>
        /// <returns></returns>
        protected Breadcrumb CreateBreadcrumb(string value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            var breadcrumb = new Breadcrumb(value)
            {
                AutoSizeAxes = Axes.X,
                RelativeSizeAxes = Axes.Y,
            };
            breadcrumb.Clicked += () => updateItems(InternalChildren.ToList().IndexOf(breadcrumb));

            return breadcrumb;
        } 

        /// <summary>
        /// The items displayed the breadcrumb navigation.
        /// </summary>
        public IReadOnlyList<string> Items
        {
            get => InternalChildren.Cast<Breadcrumb>().Select(child => child.Current.Value).ToList();
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                Clear();
                AddRange(value.Select(CreateBreadcrumb));
            }
        }

        /// <summary>
        /// Truncates the items down to the parameter newIndex.
        /// </summary>
        /// <param name="newIndex">The index where everything after will get removed</param>
        private void updateItems(int newIndex)
        {
            if (newIndex > Items.Count-1)
                throw new IndexOutOfRangeException($"Could not find an appropriate item for the index {newIndex}");
            if (newIndex < 0)
                throw new IndexOutOfRangeException("The index can not be below 0.");
            if (newIndex + 1 == Items.Count)
                return;

            Items = Items.Take(newIndex + 1).ToList();
        }

        
    }
}
