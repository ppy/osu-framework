// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Configuration;
using osu.Framework.Graphics.Containers;

namespace osu.Framework.Graphics.UserInterface
{
    /// <summary>
    /// A drop-down menu to select from a group of values.
    /// </summary>
    /// <typeparam name="T">Type of value to select.</typeparam>
    public abstract class Dropdown<T> : FillFlowContainer, IHasCurrentValue<T>
    {
        protected internal DropdownHeader Header;
        protected internal Menu DropdownMenu;

        /// <summary>
        /// Creates the header part of the control.
        /// </summary>
        protected abstract DropdownHeader CreateHeader();

        /// <summary>
        /// Creates the menu body.
        /// </summary>
        protected abstract Menu CreateMenu();

        /// <summary>
        /// Creates a menu item.
        /// </summary>
        /// <param name="text">Text to display on the menu item.</param>
        /// <param name="value">Value selected by the menu item.</param>
        protected abstract DropdownMenuItem<T> CreateMenuItem(string text, T value);

        /// <summary>
        /// A mapping from menu items to their values.
        /// </summary>
        private readonly Dictionary<T, DropdownMenuItem<T>> itemMap = new Dictionary<T, DropdownMenuItem<T>>();

        protected IEnumerable<DropdownMenuItem<T>> MenuItems => itemMap.Values;

        /// <summary>
        /// Generate menu items by <see cref="KeyValuePair{TKey, TValue}"/>.
        /// The <see cref="KeyValuePair{TKey, TValue}.Key"/> part will become <see cref="MenuItem.Text"/>,
        /// the <see cref="KeyValuePair{TKey, TValue}.Value"/> part will become <see cref="DropdownMenuItem{T}.Value"/>.
        /// </summary>
        public IEnumerable<KeyValuePair<string, T>> Items
        {
            get { return MenuItems.Select(i => new KeyValuePair<string, T>(i.Text, i.Value)); }
            set
            {
                ClearItems();
                if (value == null)
                    return;

                foreach (var entry in value)
                    AddDropdownItem(entry.Key, entry.Value);

                selectionChanged();

                if (SelectedItem == null)
                    SelectedItem = MenuItems.FirstOrDefault();
            }
        }

        /// <summary>
        /// Add a menu item directly.
        /// </summary>
        /// <param name="text">Text to display on the menu item.</param>
        /// <param name="value">Value selected by the menu item.</param>
        public void AddDropdownItem(string text, T value)
        {
            if (itemMap.ContainsKey(value))
                throw new ArgumentException($"Duplicated selections in {nameof(Dropdown<T>)}!");
            var item = CreateMenuItem(text, value);
            item.Action = () =>
            {
                selectedItem = item;
                Current.Value = item.Value;
                DropdownMenu.State = MenuState.Closed;
            };
            itemMap[item.Value] = item;
            DropdownMenu.ItemsContainer.Add(item);
        }

        // TODO: RemoveDropdownItem?

        public Bindable<T> Current { get; } = new Bindable<T>();

        private DropdownMenuItem<T> selectedItem;

        protected DropdownMenuItem<T> SelectedItem
        {
            get { return selectedItem; }
            set
            {
                selectedItem = value;
                if (value != null)
                    Current.Value = value.Value;
            }
        }

        protected Dropdown()
        {
            AutoSizeAxes = Axes.Y;
            Direction = FillDirection.Vertical;

            Children = new Drawable[]
            {
                Header = CreateHeader(),
                DropdownMenu = CreateMenu()
            };

            Header.Action = DropdownMenu.Toggle;
            Current.ValueChanged += selectionChanged;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            Header.Label = SelectedItem?.Text;
        }

        private void selectionChanged(T newSelection = default(T))
        {
            foreach (var i in MenuItems)
                i.IsSelected = false;

            // refresh if SelectedItem and SelectedValue mismatched
            // null is not a valid value for Dictionary, so neither here
            if ((SelectedItem == null || !EqualityComparer<T>.Default.Equals(SelectedItem.Value, newSelection))
                && newSelection != null)
                itemMap.TryGetValue(newSelection, out selectedItem);

            if (SelectedItem != null)
            {
                Header.Label = SelectedItem.Text;
                SelectedItem.IsSelected = true;
            }
        }

        /// <summary>
        /// Clear all the menu items.
        /// </summary>
        public void ClearItems()
        {
            itemMap.Clear();
            DropdownMenu.ItemsContainer.Clear();
        }

        /// <summary>
        /// Hide the menu item of specified value.
        /// </summary>
        /// <param name="val">The value to hide.</param>
        internal void HideItem(T val)
        {
            DropdownMenuItem<T> item;
            if (itemMap.TryGetValue(val, out item))
            {
                item.Hide();
                updateHeaderVisibility();
            }
        }

        /// <summary>
        /// Show the menu item of specified value.
        /// </summary>
        /// <param name="val">The value to show.</param>
        internal void ShowItem(T val)
        {
            DropdownMenuItem<T> item;
            if (itemMap.TryGetValue(val, out item))
            {
                item.Show();
                updateHeaderVisibility();
            }
        }

        private void updateHeaderVisibility() => Header.Alpha = MenuItems.Any(i => i.IsPresent) ? 1 : 0;
    }
}
