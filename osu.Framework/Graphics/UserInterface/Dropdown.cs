// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Configuration;
using osu.Framework.Graphics.Containers;
using osu.Framework.Input;
using OpenTK.Graphics;
using osu.Framework.Extensions.IEnumerableExtensions;

namespace osu.Framework.Graphics.UserInterface
{
    /// <summary>
    /// A drop-down menu to select from a group of values.
    /// </summary>
    /// <typeparam name="T">Type of value to select.</typeparam>
    public abstract class Dropdown<T> : FillFlowContainer, IHasCurrentValue<T>
    {
        protected internal DropdownHeader Header;
        protected internal DropdownMenu Menu;

        /// <summary>
        /// Creates the header part of the control.
        /// </summary>
        protected abstract DropdownHeader CreateHeader();

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

                if (Current.Value == null || !itemMap.Keys.Contains(Current.Value))
                    Current.Value = itemMap.Keys.FirstOrDefault();
                else
                    Current.TriggerChange();
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
                throw new ArgumentException($"The item {value} already exists in this {nameof(Dropdown<T>)}.");

            var newItem = new DropdownMenuItem<T>(text, value, () =>
            {
                if (!Current.Disabled)
                    Current.Value = value;

                Menu.State = MenuState.Closed;
            });

            Menu.Add(newItem);
            itemMap[value] = newItem;
        }

        /// <summary>
        /// Remove a menu item directly.
        /// </summary>
        /// <param name="value">Value of the menu item to be removed.</param>
        public bool RemoveDropdownItem(T value)
        {
            if (value == null)
                return false;

            if (!itemMap.TryGetValue(value, out DropdownMenuItem<T> item))
                return false;

            Menu.Remove(item);
            itemMap.Remove(value);

            return true;
        }

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
                Menu = CreateMenu()
            };

            Menu.RelativeSizeAxes = Axes.X;

            Header.Action = Menu.Toggle;
            Current.ValueChanged += selectionChanged;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            Header.Label = SelectedItem?.Text.Value;
        }

        private void selectionChanged(T newSelection = default(T))
        {
            // refresh if SelectedItem and SelectedValue mismatched
            // null is not a valid value for Dictionary, so neither here
            if ((SelectedItem == null || !EqualityComparer<T>.Default.Equals(SelectedItem.Value, newSelection))
                && newSelection != null)
            {
                if (!itemMap.TryGetValue(newSelection, out selectedItem))
                    throw new InvalidOperationException($"Attempted to update dropdown to a value which wasn't contained as an item ({newSelection}).");
            }

            Menu.SelectItem(selectedItem);
            Header.Label = selectedItem.Text;
        }

        /// <summary>
        /// Clear all the menu items.
        /// </summary>
        public void ClearItems()
        {
            itemMap.Clear();
            Menu.Clear();
        }

        /// <summary>
        /// Hide the menu item of specified value.
        /// </summary>
        /// <param name="val">The value to hide.</param>
        internal void HideItem(T val)
        {
            if (itemMap.TryGetValue(val, out DropdownMenuItem<T> item))
            {
                Menu.HideItem(item);
                updateHeaderVisibility();
            }
        }

        /// <summary>
        /// Show the menu item of specified value.
        /// </summary>
        /// <param name="val">The value to show.</param>
        internal void ShowItem(T val)
        {
            if (itemMap.TryGetValue(val, out DropdownMenuItem<T> item))
            {
                Menu.ShowItem(item);
                updateHeaderVisibility();
            }
        }

        private void updateHeaderVisibility() => Header.Alpha = Menu.AnyPresent ? 1 : 0;

        protected override bool OnHover(InputState state) => true;

        /// <summary>
        /// Creates the menu body.
        /// </summary>
        protected virtual DropdownMenu CreateMenu() => new DropdownMenu();

        #region DropdownMenu
        public class DropdownMenu : Menu
        {
            public DropdownMenu()
                : base(Direction.Vertical)
            {
            }

            /// <summary>
            /// Selects an item from this <see cref="DropdownMenu"/>.
            /// </summary>
            /// <param name="item">The item to select.</param>
            public void SelectItem(DropdownMenuItem<T> item)
            {
                Children.OfType<DrawableDropdownMenuItem>().ForEach(c => c.IsSelected = c.Item == item);
            }

            /// <summary>
            /// Shows an item from this <see cref="DropdownMenu"/>.
            /// </summary>
            /// <param name="item">The item to show.</param>
            public void HideItem(DropdownMenuItem<T> item) => Children.FirstOrDefault(c => c.Item == item)?.Hide();

            /// <summary>
            /// Hides an item from this <see cref="DropdownMenu"/>
            /// </summary>
            /// <param name="item"></param>
            public void ShowItem(DropdownMenuItem<T> item) => Children.FirstOrDefault(c => c.Item == item)?.Show();

            /// <summary>
            /// Whether any items part of this <see cref="DropdownMenu"/> are present.
            /// </summary>
            public bool AnyPresent => Children.Any(c => c.IsPresent);

            protected override DrawableMenuItem CreateDrawableMenuItem(MenuItem item) => new DrawableDropdownMenuItem(item);

            #region DrawableDropdownMenuItem
            // must be public due to mono bug(?) https://github.com/ppy/osu/issues/1204
            public class DrawableDropdownMenuItem : DrawableMenuItem
            {
                public DrawableDropdownMenuItem(MenuItem item)
                    : base(item)
                {
                }

                private bool selected;
                public bool IsSelected
                {
                    get
                    {
                        return !Item.Action.Disabled && selected;
                    }
                    set
                    {
                        if (selected == value)
                            return;
                        selected = value;

                        OnSelectChange();
                    }
                }

                private Color4 backgroundColourSelected = Color4.SlateGray;
                public Color4 BackgroundColourSelected
                {
                    get { return backgroundColourSelected; }
                    set
                    {
                        backgroundColourSelected = value;
                        UpdateBackgroundColour();
                    }
                }

                private Color4 foregroundColourSelected = Color4.White;
                public Color4 ForegroundColourSelected
                {
                    get { return foregroundColourSelected; }
                    set
                    {
                        foregroundColourSelected = value;
                        UpdateForegroundColour();
                    }
                }

                protected virtual void OnSelectChange()
                {
                    if (!IsLoaded)
                        return;

                    UpdateBackgroundColour();
                    UpdateForegroundColour();
                }

                protected override void UpdateBackgroundColour()
                {
                    Background.FadeColour(IsHovered ? BackgroundColourHover : (IsSelected ? BackgroundColourSelected : BackgroundColour));
                }

                protected override void UpdateForegroundColour()
                {
                    Foreground.FadeColour(IsHovered ? ForegroundColourHover : (IsSelected ? ForegroundColourSelected : ForegroundColour));
                }

                protected override void LoadComplete()
                {
                    base.LoadComplete();
                    Background.Colour = IsSelected ? BackgroundColourSelected : BackgroundColour;
                    Foreground.Colour = IsSelected ? ForegroundColourSelected : ForegroundColour;
                }
            }
            #endregion
        }
        #endregion
    }
}
