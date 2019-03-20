// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Bindables;
using osu.Framework.Graphics.Containers;
using osu.Framework.Input.Events;

namespace osu.Framework.Graphics.UserInterface
{
    public abstract class BreadcrumbNavigation<T> : CompositeDrawable
    {
        private readonly FillFlowContainer<Breadcrumb> fillFlowContainer;

        private readonly BindableList<T> items = new BindableList<T>();

        protected BreadcrumbNavigation()
        {
            fillFlowContainer = CreateAndAddFillFlowContainer();

            items.ItemsAdded += itemsChanged;
            items.ItemsRemoved += itemsChanged;
        }

        private void itemsChanged(IEnumerable<T> changeset)
        {
            fillFlowContainer.Clear();

            if (items.Count == 0) return;

            fillFlowContainer.AddRange(items.Select(val => {
                var breadcrumb = CreateBreadcrumb(val);

                breadcrumb.Selected += () => updateItems(fillFlowContainer.Children.ToList().IndexOf(breadcrumb));

                return breadcrumb;
            }));

            fillFlowContainer.Children.Last().Current.Value = true;
        }


        /// <summary>
        /// Override this method for customising the design of the breadcrumb.
        /// remember to set
        /// <code>
        ///    AutoSizeAxes = Axes.X,
        ///    RelativeSizeAxes = Axes.Y,
        /// </code>
        /// </summary>
        /// <param name="value">The value that is supposed to be written in the breadcrumb</param>
        protected abstract Breadcrumb CreateBreadcrumb(T value);

        /// <summary>
        /// The items displayed the breadcrumb navigation.
        /// </summary>
        public BindableList<T> Items
        {
            get => items;
            set => items.BindTo(value);
        }

        /// <summary>
        /// Creates and adds the fillflow container that contains all breadcrumbs.
        /// </summary>
        protected abstract FillFlowContainer<Breadcrumb> CreateAndAddFillFlowContainer();

        /// <summary>
        /// Truncates the items down to the parameter newIndex.
        /// </summary>
        /// <param name="newIndex">The index where everything after will get removed</param>
        private void updateItems(int newIndex)
        {
            if (newIndex > Items.Count - 1)
                throw new IndexOutOfRangeException($"Could not find an appropriate item for the index {newIndex}");
            if (newIndex < 0)
                throw new IndexOutOfRangeException("The index can not be below 0.");
            if (newIndex + 1 == Items.Count)
                return;

            for (int i = Items.Count - 1; i != newIndex; i--)
                Items.RemoveAt(i);

            foreach (var unselected in fillFlowContainer.Children.Take(newIndex))
                unselected.Current.Value = false;

            fillFlowContainer.Children.Last().Current.Value = true;
        }

        protected abstract class Breadcrumb : CompositeDrawable, IHasCurrentValue<bool>
        {
            private readonly Bindable<bool> current = new Bindable<bool>();
            public Bindable<bool> Current {
                get => current;
                set => current.BindTo(value);
            }

            public T Value { get; }

            protected Breadcrumb(T value)
            {
                Value = value;
            }

            protected override bool OnClick(ClickEvent e)
            {
                Selected?.Invoke();

                return true;
            }

            public event Action Selected;
        }
    }
}
