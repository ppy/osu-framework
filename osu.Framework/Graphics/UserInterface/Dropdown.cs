// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Bindables;
using osu.Framework.Extensions;
using osu.Framework.Graphics.Containers;
using osuTK.Graphics;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Graphics.Sprites;

namespace osu.Framework.Graphics.UserInterface
{
    /// <summary>
    /// A drop-down menu to select from a group of values.
    /// </summary>
    /// <typeparam name="T">Type of value to select.</typeparam>
    public abstract class Dropdown<T> : CompositeDrawable, IHasCurrentValue<T>
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
        /// Enumerate all values in the dropdown.
        /// </summary>
        public IEnumerable<T> Items
        {
            get => MenuItems.Select(i => i.Value);
            set
            {
                if (boundItemSource != null)
                    throw new InvalidOperationException($"Cannot manually set {nameof(Items)} when an {nameof(ItemSource)} is bound.");

                setItems(value);
            }
        }

        private void setItems(IEnumerable<T> items)
        {
            clearItems();
            if (items == null)
                return;

            foreach (var entry in items)
                addDropdownItem(GenerateItemText(entry), entry);

            if (Current.Value == null || !itemMap.Keys.Contains(Current.Value))
                Current.Value = itemMap.Keys.FirstOrDefault();
            else
                Current.TriggerChange();
        }

        private readonly IBindableList<T> itemSource = new BindableList<T>();
        private IBindableList<T> boundItemSource;

        /// <summary>
        /// Allows the developer to assign an <see cref="IBindableList{T}"/> as the source
        /// of items for this dropdown.
        /// </summary>
        public IBindableList<T> ItemSource
        {
            get => itemSource;
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                if (boundItemSource != null) itemSource.UnbindFrom(boundItemSource);
                itemSource.BindTo(boundItemSource = value);
            }
        }

        /// <summary>
        /// Add a menu item directly while automatically generating a label.
        /// </summary>
        /// <param name="value">Value selected by the menu item.</param>
        public void AddDropdownItem(T value) => AddDropdownItem(GenerateItemText(value), value);

        /// <summary>
        /// Add a menu item directly.
        /// </summary>
        /// <param name="text">Text to display on the menu item.</param>
        /// <param name="value">Value selected by the menu item.</param>
        protected void AddDropdownItem(string text, T value)
        {
            if (boundItemSource != null)
                throw new InvalidOperationException($"Cannot manually add dropdown items when an {nameof(ItemSource)} is bound.");

            addDropdownItem(text, value);
        }

        private void addDropdownItem(string text, T value)
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
            if (boundItemSource != null)
                throw new InvalidOperationException($"Cannot manually remove items when an {nameof(ItemSource)} is bound.");

            return removeDropdownItem(value);
        }

        private bool removeDropdownItem(T value)
        {
            if (value == null)
                return false;

            if (!itemMap.TryGetValue(value, out var item))
                return false;

            Menu.Remove(item);
            itemMap.Remove(value);

            return true;
        }

        protected virtual string GenerateItemText(T item)
        {
            switch (item)
            {
                case MenuItem i:
                    return i.Text.Value;

                case IHasText t:
                    return t.Text;

                case Enum e:
                    return e.GetDescription();

                default:
                    return item?.ToString() ?? "null";
            }
        }

        private readonly Bindable<T> current = new Bindable<T>();

        private Bindable<T> currentBound;

        public Bindable<T> Current
        {
            get => current;
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                if (currentBound != null) current.UnbindFrom(currentBound);
                current.BindTo(currentBound = value);
            }
        }

        private DropdownMenuItem<T> selectedItem;

        protected DropdownMenuItem<T> SelectedItem
        {
            get => selectedItem;
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

            InternalChild = new FillFlowContainer<Drawable>
            {
                Children = new Drawable[]
                {
                    Header = CreateHeader(),
                    Menu = CreateMenu()
                },
                Direction = FillDirection.Vertical,
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y
            };

            Menu.RelativeSizeAxes = Axes.X;

            Header.Action = Menu.Toggle;
            Current.ValueChanged += selectionChanged;

            ItemSource.ItemsAdded += _ => setItems(ItemSource);
            ItemSource.ItemsRemoved += _ => setItems(ItemSource);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            Header.Label = SelectedItem?.Text.Value;
        }

        private void selectionChanged(ValueChangedEvent<T> args)
        {
            // refresh if SelectedItem and SelectedValue mismatched
            // null is not a valid value for Dictionary, so neither here
            if (args.NewValue == null && SelectedItem != null)
            {
                selectedItem = new DropdownMenuItem<T>(null, default);
            }
            else if (SelectedItem == null || !EqualityComparer<T>.Default.Equals(SelectedItem.Value, args.NewValue))
            {
                if (!itemMap.TryGetValue(args.NewValue, out selectedItem))
                {
                    selectedItem = new DropdownMenuItem<T>(GenerateItemText(args.NewValue), args.NewValue);
                }
            }

            Menu.SelectItem(selectedItem);
            Header.Label = selectedItem.Text.Value;
        }

        /// <summary>
        /// Clear all the menu items.
        /// </summary>
        public void ClearItems()
        {
            if (boundItemSource != null)
                throw new InvalidOperationException($"Cannot manually clear items when an {nameof(ItemSource)} is bound.");

            clearItems();
        }

        private void clearItems()
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

        /// <summary>
        /// Creates the menu body.
        /// </summary>
        protected abstract DropdownMenu CreateMenu();

        #region DropdownMenu

        public abstract class DropdownMenu : Menu
        {
            protected DropdownMenu()
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

            protected sealed override DrawableMenuItem CreateDrawableMenuItem(MenuItem item) => CreateDrawableDropdownMenuItem(item);

            protected abstract DrawableDropdownMenuItem CreateDrawableDropdownMenuItem(MenuItem item);

            #region DrawableDropdownMenuItem

            // must be public due to mono bug(?) https://github.com/ppy/osu/issues/1204
            public abstract class DrawableDropdownMenuItem : DrawableMenuItem
            {
                protected DrawableDropdownMenuItem(MenuItem item)
                    : base(item)
                {
                }

                private bool selected;

                public bool IsSelected
                {
                    get => !Item.Action.Disabled && selected;
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
                    get => backgroundColourSelected;
                    set
                    {
                        backgroundColourSelected = value;
                        UpdateBackgroundColour();
                    }
                }

                private Color4 foregroundColourSelected = Color4.White;

                public Color4 ForegroundColourSelected
                {
                    get => foregroundColourSelected;
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
                    Background.FadeColour(IsHovered ? BackgroundColourHover : IsSelected ? BackgroundColourSelected : BackgroundColour);
                }

                protected override void UpdateForegroundColour()
                {
                    Foreground.FadeColour(IsHovered ? ForegroundColourHover : IsSelected ? ForegroundColourSelected : ForegroundColour);
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
