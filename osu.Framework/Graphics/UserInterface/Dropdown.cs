// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Bindables;
using osu.Framework.Extensions;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osuTK.Graphics;
using osuTK.Input;

namespace osu.Framework.Graphics.UserInterface
{
    /// <summary>
    /// A drop-down menu to select from a group of values.
    /// </summary>
    /// <typeparam name="T">Type of value to select.</typeparam>
    public abstract partial class Dropdown<T> : CompositeDrawable, IHasCurrentValue<T>
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
                addDropdownItem(entry);

            if (Current.Value == null || !itemMap.Keys.Contains(Current.Value, EqualityComparer<T>.Default))
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
        public void AddDropdownItem(T value)
        {
            if (boundItemSource != null)
                throw new InvalidOperationException($"Cannot manually add dropdown items when an {nameof(ItemSource)} is bound.");

            addDropdownItem(value);
        }

        private void addDropdownItem(T value)
        {
            if (itemMap.ContainsKey(value))
                throw new ArgumentException($"The item {value} already exists in this {nameof(Dropdown<T>)}.");

            var newItem = new DropdownMenuItem<T>(value, () =>
            {
                if (!Current.Disabled)
                    Current.Value = value;

                Menu.State = MenuState.Closed;
            });

            // inheritors expect that `virtual GenerateItemText` is only called when this dropdown is fully loaded.
            if (IsLoaded)
                newItem.Text.Value = GenerateItemText(value);

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

        /// <summary>
        /// Called to generate the text to be shown for this <paramref name="item"/>.
        /// </summary>
        /// <remarks>
        /// Can be overriden if custom behaviour is needed. Will only be called after this <see cref="Dropdown{T}"/> has fully loaded.
        /// </remarks>
        protected virtual LocalisableString GenerateItemText(T item)
        {
            switch (item)
            {
                case MenuItem i:
                    return i.Text.Value;

                case IHasText t:
                    return t.Text;

                case Enum e:
                    return e.GetLocalisableDescription();

                default:
                    return item?.ToString() ?? "null";
            }
        }

        private readonly BindableWithCurrent<T> current = new BindableWithCurrent<T>();

        public Bindable<T> Current
        {
            get => current.Current;
            set => current.Current = value;
        }

        private DropdownMenuItem<T> selectedItem;

        protected DropdownMenuItem<T> SelectedItem
        {
            get => selectedItem;
            set
            {
                if (Current.Disabled)
                    return;

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
            Header.ChangeSelection += selectionKeyPressed;
            Menu.PreselectionConfirmed += preselectionConfirmed;
            Current.ValueChanged += val => Scheduler.AddOnce(selectionChanged, val);
            Current.DisabledChanged += disabled =>
            {
                Header.Enabled.Value = !disabled;
                if (disabled && Menu.State == MenuState.Open)
                    Menu.State = MenuState.Closed;
            };

            ItemSource.CollectionChanged += (_, _) => setItems(itemSource);
        }

        private void preselectionConfirmed(int selectedIndex)
        {
            SelectedItem = MenuItems.ElementAtOrDefault(selectedIndex);
            Menu.State = MenuState.Closed;
        }

        private void selectionKeyPressed(DropdownHeader.DropdownSelectionAction action)
        {
            if (!MenuItems.Any())
                return;

            var dropdownMenuItems = MenuItems.ToList();

            switch (action)
            {
                case DropdownHeader.DropdownSelectionAction.Previous:
                    SelectedItem = dropdownMenuItems[Math.Clamp(dropdownMenuItems.IndexOf(SelectedItem) - 1, 0, dropdownMenuItems.Count - 1)];
                    break;

                case DropdownHeader.DropdownSelectionAction.Next:
                    SelectedItem = dropdownMenuItems[Math.Clamp(dropdownMenuItems.IndexOf(SelectedItem) + 1, 0, dropdownMenuItems.Count - 1)];
                    break;

                case DropdownHeader.DropdownSelectionAction.First:
                    SelectedItem = dropdownMenuItems[0];
                    break;

                case DropdownHeader.DropdownSelectionAction.Last:
                    SelectedItem = dropdownMenuItems[^1];
                    break;

                default:
                    throw new ArgumentException("Unexpected selection action type.", nameof(action));
            }
        }

        protected override void LoadAsyncComplete()
        {
            base.LoadAsyncComplete();

            foreach (var item in MenuItems)
                item.Text.Value = GenerateItemText(item.Value);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            Header.Label = SelectedItem?.Text.Value ?? default;
        }

        private void selectionChanged(ValueChangedEvent<T> args)
        {
            // refresh if SelectedItem and SelectedValue mismatched
            // null is not a valid value for Dictionary, so neither here
            if (args.NewValue == null && SelectedItem != null)
            {
                selectedItem = new DropdownMenuItem<T>(default(LocalisableString), default);
            }
            else if (SelectedItem == null || !EqualityComparer<T>.Default.Equals(SelectedItem.Value, args.NewValue))
            {
                if (args.NewValue == null || !itemMap.TryGetValue(args.NewValue, out selectedItem))
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

        public abstract partial class DropdownMenu : Menu, IKeyBindingHandler<PlatformAction>
        {
            protected DropdownMenu()
                : base(Direction.Vertical)
            {
                StateChanged += clearPreselection;
            }

            public override void Add(MenuItem item)
            {
                base.Add(item);

                var drawableDropdownMenuItem = (DrawableDropdownMenuItem)ItemsContainer.Single(drawableItem => drawableItem.Item == item);
                drawableDropdownMenuItem.PreselectionRequested += PreselectItem;
            }

            private void clearPreselection(MenuState obj)
            {
                if (obj == MenuState.Closed)
                    PreselectItem(null);
            }

            protected internal IEnumerable<DrawableDropdownMenuItem> DrawableMenuItems => Children.OfType<DrawableDropdownMenuItem>();
            protected internal IEnumerable<DrawableDropdownMenuItem> VisibleMenuItems => DrawableMenuItems.Where(item => !item.IsMaskedAway);

            public DrawableDropdownMenuItem PreselectedItem => DrawableMenuItems.FirstOrDefault(c => c.IsPreSelected)
                                                               ?? DrawableMenuItems.FirstOrDefault(c => c.IsSelected);

            public event Action<int> PreselectionConfirmed;

            /// <summary>
            /// Selects an item from this <see cref="DropdownMenu"/>.
            /// </summary>
            /// <param name="item">The item to select.</param>
            public void SelectItem(DropdownMenuItem<T> item)
            {
                Children.OfType<DrawableDropdownMenuItem>().ForEach(c =>
                {
                    c.IsSelected = compareItemEquality(item, c.Item);
                    if (c.IsSelected)
                        ContentContainer.ScrollIntoView(c);
                });
            }

            /// <summary>
            /// Shows an item from this <see cref="DropdownMenu"/>.
            /// </summary>
            /// <param name="item">The item to show.</param>
            public void HideItem(DropdownMenuItem<T> item) => Children.FirstOrDefault(c => compareItemEquality(item, c.Item))?.Hide();

            /// <summary>
            /// Hides an item from this <see cref="DropdownMenu"/>
            /// </summary>
            /// <param name="item"></param>
            public void ShowItem(DropdownMenuItem<T> item) => Children.FirstOrDefault(c => compareItemEquality(item, c.Item))?.Show();

            /// <summary>
            /// Whether any items part of this <see cref="DropdownMenu"/> are present.
            /// </summary>
            public bool AnyPresent => Children.Any(c => c.IsPresent);

            protected void PreselectItem(int index) => PreselectItem(Items[Math.Clamp(index, 0, DrawableMenuItems.Count() - 1)]);

            /// <summary>
            /// Preselects an item from this <see cref="DropdownMenu"/>.
            /// </summary>
            /// <param name="item">The item to select.</param>
            protected void PreselectItem(MenuItem item)
            {
                Children.OfType<DrawableDropdownMenuItem>().ForEach(c =>
                {
                    c.IsPreSelected = compareItemEquality(item, c.Item);
                    if (c.IsPreSelected)
                        ContentContainer.ScrollIntoView(c);
                });
            }

            protected sealed override DrawableMenuItem CreateDrawableMenuItem(MenuItem item) => CreateDrawableDropdownMenuItem(item);

            protected abstract DrawableDropdownMenuItem CreateDrawableDropdownMenuItem(MenuItem item);

            private static bool compareItemEquality(MenuItem a, MenuItem b)
            {
                if (a is not DropdownMenuItem<T> aTyped || b is not DropdownMenuItem<T> bTyped)
                    return false;

                return EqualityComparer<T>.Default.Equals(aTyped.Value, bTyped.Value);
            }

            #region DrawableDropdownMenuItem

            public abstract partial class DrawableDropdownMenuItem : DrawableMenuItem
            {
                public event Action<DropdownMenuItem<T>> PreselectionRequested;

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

                private bool preSelected;

                /// <summary>
                /// Denotes whether this menu item will be selected on <see cref="Key.Enter"/> press.
                /// This property is related to selecting menu items using keyboard or hovering.
                /// </summary>
                public bool IsPreSelected
                {
                    get => preSelected;
                    set
                    {
                        if (preSelected == value)
                            return;

                        preSelected = value;

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
                        Scheduler.AddOnce(UpdateBackgroundColour);
                    }
                }

                private Color4 foregroundColourSelected = Color4.White;

                public Color4 ForegroundColourSelected
                {
                    get => foregroundColourSelected;
                    set
                    {
                        foregroundColourSelected = value;
                        Scheduler.AddOnce(UpdateForegroundColour);
                    }
                }

                protected virtual void OnSelectChange()
                {
                    Scheduler.AddOnce(UpdateBackgroundColour);
                    Scheduler.AddOnce(UpdateForegroundColour);
                }

                protected override void UpdateBackgroundColour()
                {
                    Background.FadeColour(IsPreSelected ? BackgroundColourHover : IsSelected ? BackgroundColourSelected : BackgroundColour);
                }

                protected override void UpdateForegroundColour()
                {
                    Foreground.FadeColour(IsPreSelected ? ForegroundColourHover : IsSelected ? ForegroundColourSelected : ForegroundColour);
                }

                protected override bool OnHover(HoverEvent e)
                {
                    PreselectionRequested?.Invoke(Item as DropdownMenuItem<T>);
                    return base.OnHover(e);
                }
            }

            #endregion

            protected override bool OnKeyDown(KeyDownEvent e)
            {
                var drawableMenuItemsList = DrawableMenuItems.ToList();
                if (!drawableMenuItemsList.Any())
                    return base.OnKeyDown(e);

                var currentPreselected = PreselectedItem;
                int targetPreselectionIndex = drawableMenuItemsList.IndexOf(currentPreselected);

                switch (e.Key)
                {
                    case Key.Up:
                        PreselectItem(targetPreselectionIndex - 1);
                        return true;

                    case Key.Down:
                        PreselectItem(targetPreselectionIndex + 1);
                        return true;

                    case Key.PageUp:
                        var firstVisibleItem = VisibleMenuItems.First();

                        if (currentPreselected == firstVisibleItem)
                            PreselectItem(targetPreselectionIndex - VisibleMenuItems.Count());
                        else
                            PreselectItem(drawableMenuItemsList.IndexOf(firstVisibleItem));
                        return true;

                    case Key.PageDown:
                        var lastVisibleItem = VisibleMenuItems.Last();

                        if (currentPreselected == lastVisibleItem)
                            PreselectItem(targetPreselectionIndex + VisibleMenuItems.Count());
                        else
                            PreselectItem(drawableMenuItemsList.IndexOf(lastVisibleItem));
                        return true;

                    case Key.Enter:
                        PreselectionConfirmed?.Invoke(targetPreselectionIndex);
                        return true;

                    case Key.Escape:
                        State = MenuState.Closed;
                        return true;

                    default:
                        return base.OnKeyDown(e);
                }
            }

            public bool OnPressed(KeyBindingPressEvent<PlatformAction> e)
            {
                switch (e.Action)
                {
                    case PlatformAction.MoveToListStart:
                        PreselectItem(Items.FirstOrDefault());
                        return true;

                    case PlatformAction.MoveToListEnd:
                        PreselectItem(Items.LastOrDefault());
                        return true;

                    default:
                        return false;
                }
            }

            public void OnReleased(KeyBindingReleaseEvent<PlatformAction> e)
            {
            }
        }

        #endregion
    }
}
