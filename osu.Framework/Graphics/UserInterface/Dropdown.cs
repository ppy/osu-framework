// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Framework.Layout;
using osu.Framework.Localisation;
using osuTK.Graphics;
using osuTK.Input;

namespace osu.Framework.Graphics.UserInterface
{
    /// <summary>
    /// A drop-down menu to select from a group of values.
    /// </summary>
    /// <typeparam name="T">Type of value to select.</typeparam>
    [Cached(typeof(IDropdown))]
    public abstract partial class Dropdown<T> : CompositeDrawable, IHasCurrentValue<T>, IFocusManager, IDropdown
    {
        protected internal DropdownHeader Header;
        protected internal DropdownMenu Menu;

        /// <summary>
        /// Whether this <see cref="Dropdown{T}"/> should always have a search bar displayed in the header when opened.
        /// </summary>
        public bool AlwaysShowSearchBar
        {
            get => Header.AlwaysShowSearchBar;
            set => Header.AlwaysShowSearchBar = value;
        }

        public bool AllowNonContiguousMatching
        {
            get => Menu.AllowNonContiguousMatching;
            set => Menu.AllowNonContiguousMatching = value;
        }

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
                ArgumentNullException.ThrowIfNull(value);

                if (boundItemSource != null) itemSource.UnbindFrom(boundItemSource);
                itemSource.BindTo(boundItemSource = value);

                setItems(value);
            }
        }

        private readonly BindableBool enabled = new BindableBool(true);

        private void setItems(IEnumerable<T> value)
        {
            clearItems();

            if (value == null)
                return;

            foreach (var entry in value)
                addDropdownItem(entry);

            ensureItemSelectionIsValid();
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

        private void addDropdownItem(T value, int? position = null)
        {
            if (itemMap.ContainsKey(value))
                throw new ArgumentException($"The item {value} already exists in this {nameof(Dropdown<T>)}.");

            var item = new DropdownMenuItem<T>(value, () =>
            {
                if (!Current.Disabled)
                    Current.Value = value;

                Menu.Close();
            });

            // inheritors expect that `virtual GenerateItemText` is only called when this dropdown's BDL has run to completion.
            if (LoadState >= LoadState.Ready)
                item.Text.Value = GenerateItemText(value);

            if (position != null)
                Menu.Insert(position.Value, item);
            else
                Menu.Add(item);

            itemMap[value] = item;
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

        /// <summary>
        /// Puts the state of this <see cref="Dropdown{T}"/> one level back:
        ///  - If the dropdown search bar contains text, this method will reset it.
        ///  - If the dropdown is open, this method wil close it.
        /// </summary>
        public bool Back()
        {
            if (Header.SearchBar.Back())
                return true;

            if (Menu.State == MenuState.Open)
            {
                Menu.Close();
                return true;
            }

            return false;
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

            Header.ChangeSelection += selectionKeyPressed;
            Header.SearchTerm.ValueChanged += t => Menu.SearchTerm = t.NewValue;

            Menu.RelativeSizeAxes = Axes.X;
            Menu.FilterCompleted += filterCompleted;

            Current.ValueChanged += val => Scheduler.AddOnce(updateItemSelection, val.NewValue);
            Current.DisabledChanged += disabled =>
            {
                enabled.Value = !disabled;
                if (disabled && Menu.State == MenuState.Open)
                    Menu.State = MenuState.Closed;
            };

            ItemSource.CollectionChanged += collectionChanged;
        }

        private void filterCompleted()
        {
            if (!string.IsNullOrEmpty(Menu.SearchTerm))
                Menu.PreselectItem(0);
            else
                Menu.PreselectItem(null);
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
            {
                Debug.Assert(string.IsNullOrEmpty(item.Text.Value.ToString()));
                item.Text.Value = GenerateItemText(item.Value);
            }
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            Header.Label = SelectedItem?.Text.Value ?? default;
        }

        protected override bool OnKeyDown(KeyDownEvent e)
        {
            if (e.Key == Key.Escape)
                return Back();

            return false;
        }

        private void collectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                {
                    var newItems = e.NewItems.AsNonNull().Cast<T>().ToArray();

                    for (int i = 0; i < newItems.Length; i++)
                        addDropdownItem(newItems[i], e.NewStartingIndex + i);

                    break;
                }

                case NotifyCollectionChangedAction.Remove:
                    foreach (var item in e.OldItems.AsNonNull().Cast<T>())
                        removeDropdownItem(item);

                    break;

                case NotifyCollectionChangedAction.Move:
                {
                    var item = Items.ElementAt(e.OldStartingIndex);
                    removeDropdownItem(item);
                    addDropdownItem(item, e.NewStartingIndex);
                    break;
                }

                case NotifyCollectionChangedAction.Replace:
                {
                    foreach (var item in e.OldItems.AsNonNull().Cast<T>())
                        removeDropdownItem(item);

                    var newItems = e.NewItems.AsNonNull().Cast<T>().ToArray();

                    for (int i = 0; i < newItems.Length; i++)
                        addDropdownItem(newItems[i], e.NewStartingIndex + i);

                    break;
                }

                case NotifyCollectionChangedAction.Reset:
                    clearItems();
                    break;
            }

            ensureItemSelectionIsValid();
        }

        private void ensureItemSelectionIsValid()
        {
            if (Current.Value == null || !itemMap.ContainsKey(Current.Value))
            {
                Current.Value = itemMap.Keys.FirstOrDefault();
                return;
            }

            updateItemSelection(Current.Value);
        }

        private void updateItemSelection(T value)
        {
            if (value != null && itemMap.TryGetValue(value, out var existingItem))
                selectedItem = existingItem;
            else
            {
                if (value == null && selectedItem != null)
                    selectedItem = new DropdownMenuItem<T>(default(LocalisableString), default);
                else
                    selectedItem = new DropdownMenuItem<T>(GenerateItemText(value), value);
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
            private SearchContainer<DrawableMenuItem> itemsFlow;

            /// <summary>
            /// Search terms to filter items displayed in this menu.
            /// </summary>
            public string SearchTerm
            {
                get => itemsFlow.SearchTerm;
                set => itemsFlow.SearchTerm = value;
            }

            public bool AllowNonContiguousMatching
            {
                get => itemsFlow.AllowNonContiguousMatching;
                set => itemsFlow.AllowNonContiguousMatching = value;
            }

            public event Action FilterCompleted
            {
                add => itemsFlow.FilterCompleted += value;
                remove => itemsFlow.FilterCompleted -= value;
            }

            protected DropdownMenu()
                : base(Direction.Vertical)
            {
                StateChanged += clearPreselection;
            }

            private void clearPreselection(MenuState obj)
            {
                if (obj == MenuState.Closed)
                    PreselectItem(null);
            }

            protected internal IEnumerable<DrawableDropdownMenuItem> VisibleMenuItems => Children.OfType<DrawableDropdownMenuItem>().Where(i => i.MatchingFilter);
            protected internal IEnumerable<DrawableDropdownMenuItem> MenuItemsInView => VisibleMenuItems.Where(item => !item.IsMaskedAway);

            public DrawableDropdownMenuItem PreselectedItem => VisibleMenuItems.FirstOrDefault(c => c.IsPreSelected)
                                                               ?? VisibleMenuItems.FirstOrDefault(c => c.IsSelected);

            /// <summary>
            /// Selects an item from this <see cref="DropdownMenu"/>.
            /// </summary>
            /// <param name="item">The item to select.</param>
            public void SelectItem(DropdownMenuItem<T> item)
            {
                Children.OfType<DrawableDropdownMenuItem>().ForEach(c =>
                {
                    bool wasSelected = c.IsSelected;
                    c.IsSelected = compareItemEquality(item, c.Item);
                    if (c.IsSelected && !wasSelected)
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

            protected internal void PreselectItem(int index)
            {
                PreselectItem(VisibleMenuItems.Any()
                    ? VisibleMenuItems.ElementAt(Math.Clamp(index, 0, VisibleMenuItems.Count() - 1)).Item
                    : null);
            }

            /// <summary>
            /// Preselects an item from this <see cref="DropdownMenu"/>.
            /// </summary>
            /// <param name="item">The item to select.</param>
            protected internal void PreselectItem(MenuItem item)
            {
                Children.OfType<DrawableDropdownMenuItem>().ForEach(c =>
                {
                    bool wasPreSelected = c.IsPreSelected;
                    c.IsPreSelected = compareItemEquality(item, c.Item);
                    if (c.IsPreSelected && !wasPreSelected)
                        ContentContainer.ScrollIntoView(c);
                });
            }

            protected sealed override DrawableMenuItem CreateDrawableMenuItem(MenuItem item)
            {
                var drawableItem = CreateDrawableDropdownMenuItem(item);
                drawableItem.PreselectionRequested += PreselectItem;
                return drawableItem;
            }

            protected abstract DrawableDropdownMenuItem CreateDrawableDropdownMenuItem(MenuItem item);

            private static bool compareItemEquality(MenuItem a, MenuItem b)
            {
                if (a is not DropdownMenuItem<T> aTyped || b is not DropdownMenuItem<T> bTyped)
                    return false;

                return EqualityComparer<T>.Default.Equals(aTyped.Value, bTyped.Value);
            }

            #region DrawableDropdownMenuItem

            public abstract partial class DrawableDropdownMenuItem : DrawableMenuItem, IFilterable
            {
                public event Action<DropdownMenuItem<T>> PreselectionRequested;

                private bool matchingFilter = true;

                public bool MatchingFilter
                {
                    get => matchingFilter;
                    set
                    {
                        matchingFilter = value;
                        UpdateFilteringState(value);
                    }
                }

                public virtual bool FilteringActive
                {
                    set { }
                }

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

                protected virtual void UpdateFilteringState(bool filtered) => this.FadeTo(filtered ? 1 : 0);

                protected override bool OnHover(HoverEvent e)
                {
                    PreselectionRequested?.Invoke(Item as DropdownMenuItem<T>);
                    return base.OnHover(e);
                }
            }

            #endregion

            protected override bool OnKeyDown(KeyDownEvent e)
            {
                var visibleMenuItemsList = VisibleMenuItems.ToList();

                if (visibleMenuItemsList.Count > 0)
                {
                    var currentPreselected = PreselectedItem;
                    int targetPreselectionIndex = visibleMenuItemsList.IndexOf(currentPreselected);

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
                                PreselectItem(visibleMenuItemsList.IndexOf(firstVisibleItem));
                            return true;

                        case Key.PageDown:
                            var lastVisibleItem = VisibleMenuItems.Last();

                            if (currentPreselected == lastVisibleItem)
                                PreselectItem(targetPreselectionIndex + VisibleMenuItems.Count());
                            else
                                PreselectItem(visibleMenuItemsList.IndexOf(lastVisibleItem));
                            return true;
                    }
                }

                if (e.Key == Key.Escape)
                    // we'll handle closing the menu in Dropdown instead,
                    // since a search bar may be active and we want to reset it rather than closing the menu.
                    return false;

                return base.OnKeyDown(e);
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

            internal override IItemsFlow CreateItemsFlow(FillDirection direction) => (IItemsFlow)(itemsFlow = new SearchableItemsFlow
            {
                Direction = direction,
            });

            private partial class SearchableItemsFlow : SearchContainer<DrawableMenuItem>, IItemsFlow
            {
                public LayoutValue SizeCache { get; } = new LayoutValue(Invalidation.RequiredParentSizeToFit, InvalidationSource.Self);

                public SearchableItemsFlow()
                {
                    AddLayout(SizeCache);
                }
            }
        }

        #endregion

        #region IFocusManager

        // Isolate input so that the Menu doesn't disturb focus. Focus is managed via the IDropdown interface.
        void IFocusManager.TriggerFocusContention(Drawable triggerSource) { }

        // Isolate input so that the Menu doesn't disturb focus. Focus is managed via the IDropdown interface.
        bool IFocusManager.ChangeFocus(Drawable potentialFocusTarget) => false;

        #endregion

        #region IDropdown

        event Action<MenuState> IDropdown.MenuStateChanged
        {
            add => Menu.StateChanged += value;
            remove => Menu.StateChanged -= value;
        }

        IBindable<bool> IDropdown.Enabled => enabled;

        MenuState IDropdown.MenuState => Menu.State;

        void IDropdown.ToggleMenu()
        {
            if (!Current.Disabled)
                Menu.Toggle();
        }

        void IDropdown.OpenMenu()
        {
            if (!Current.Disabled)
                Menu.State = MenuState.Open;
        }

        void IDropdown.CloseMenu()
        {
            if (!Current.Disabled)
                Menu.State = MenuState.Closed;
        }

        void IDropdown.CommitPreselection()
        {
            if (Current.Disabled)
                return;

            var visibleMenuItemsList = Menu.VisibleMenuItems.ToList();

            if (visibleMenuItemsList.Count == 0)
                return;

            int targetPreselectionIndex = visibleMenuItemsList.IndexOf(Menu.PreselectedItem);
            var preselectedItem = Menu.VisibleMenuItems.ElementAt(targetPreselectionIndex);

            SelectedItem = (DropdownMenuItem<T>)preselectedItem.Item;
        }

        void IDropdown.TriggerFocusContention(Drawable triggerSource) => GetContainingFocusManager()?.TriggerFocusContention(triggerSource);

        bool IDropdown.ChangeFocus(Drawable potentialFocusTarget) => GetContainingFocusManager()?.ChangeFocus(potentialFocusTarget) ?? false;

        #endregion
    }
}
