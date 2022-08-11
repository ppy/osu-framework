// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;
using osu.Framework.Bindables;
using osu.Framework.Extensions.EnumExtensions;
using osu.Framework.Graphics.Containers;
using osu.Framework.Input;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osuTK;

namespace osu.Framework.Graphics.UserInterface
{
    /// <summary>
    /// A single-row control to display a list of selectable tabs along with an optional right-aligned dropdown
    /// containing overflow items (tabs which cannot be displayed in the allocated width). Includes
    /// support for pinning items, causing them to be displayed before all other items at the
    /// start of the list.
    /// </summary>
    /// <remarks>
    /// If a multi-line (or vertical) tab control is required, <see cref="TabFillFlowContainer.AllowMultiline"/> must be set to true.
    /// Without this, <see cref="TabControl{T}"/> will automatically hide extra items.
    /// </remarks>
    /// <typeparam name="T">The type of item to be represented by tabs.</typeparam>
    public abstract class TabControl<T> : CompositeDrawable, IHasCurrentValue<T>, IKeyBindingHandler<PlatformAction>
    {
        private readonly BindableWithCurrent<T> current = new BindableWithCurrent<T>();

        public Bindable<T> Current
        {
            get => current.Current;
            set => current.Current = value;
        }

        /// <summary>
        /// A collection of all tabs which are valid switch targets, sorted by their order of appearance.
        /// </summary>
        protected internal IEnumerable<TabItem<T>> SwitchableTabs => AllTabs.Where(tab => tab.IsSwitchable);

        /// <summary>
        /// The collection of all tabs, sorted by their order of appearance.
        /// </summary>
        protected internal IEnumerable<TabItem<T>> AllTabs => TabContainer.AllTabItems;

        /// <summary>
        /// All items which are currently present and visible in the tab control.
        /// </summary>
        public IEnumerable<T> VisibleItems => TabContainer.TabItems.Select(t => t.Value).Distinct();

        private readonly List<T> items = new List<T>();

        /// <summary>
        /// The list of all items contained by this <see cref="TabControl{T}"/>.
        /// </summary>
        [NotNull]
        public IReadOnlyList<T> Items
        {
            get => items;
            set
            {
                foreach (var item in items.ToList())
                    RemoveItem(item);

                foreach (var item in value)
                    AddItem(item);
            }
        }

        /// <summary>
        /// When true, tabs selected from the overflow dropdown will be moved to the front of the list (after pinned items).
        /// </summary>
        public bool AutoSort { set; get; }

        /// <summary>
        /// The <see cref="Dropdown{T}"/> which is displayed when tabs overflow the visible bounds of this <see cref="TabControl{T}"/>.
        /// </summary>
        [CanBeNull]
        protected readonly Dropdown<T> Dropdown;

        /// <summary>
        /// The flow of <see cref="TabItem{T}"/>s.
        /// </summary>
        protected readonly TabFillFlowContainer TabContainer;

        /// <summary>
        /// The currently-selected <see cref="TabItem{T}"/>.
        /// </summary>
        protected TabItem<T> SelectedTab { get; private set; }

        /// <summary>
        /// When <c>true</c>, the first available tab (if any) will be selected at the point of <see cref="LoadComplete"/>.
        /// </summary>
        public bool SelectFirstTabByDefault { get; set; } = true;

        /// <summary>
        /// When true, tabs can be switched back and forth using <see cref="PlatformAction.DocumentPrevious"/> and <see cref="PlatformAction.DocumentNext"/> respectively.
        /// </summary>
        public bool IsSwitchable { get; set; }

        /// <summary>
        /// Whether a new tab should be automatically switched to when the current tab is removed.
        /// </summary>
        /// <remarks>
        /// When <c>true</c>:
        /// <list type="bullet">
        /// <item>
        /// <description>If the current tab is not the only tab in the <see cref="TabControl{T}"/>, then the next or previous tab will be selected depending on the current tab's position.</description>
        /// </item>
        /// <item>
        /// <description>If the current tab is the only tab in the <see cref="TabControl{T}"/>, then selection will be cleared.</description>
        /// </item>
        /// </list>
        /// </remarks>
        public bool SwitchTabOnRemove { get; set; } = true;

        /// <summary>
        /// Creates an optional overflow dropdown.
        /// When implementing this dropdown make sure:
        /// <list type="bullet">
        /// <item>
        /// <description>It is made to be anchored to the right-hand side of its parent.</description>
        /// </item>
        /// <item>
        /// <description>The dropdown's header does *not* have a relative x axis.</description>
        /// </item>
        /// </list>
        /// </summary>
        protected abstract Dropdown<T> CreateDropdown();

        /// <summary>
        /// Creates a <see cref="TabItem{T}"/> for a given <typeparamref name="T"/> value.
        /// </summary>
        protected abstract TabItem<T> CreateTabItem(T value);

        /// <summary>
        /// Decremented each time a tab needs to be inserted at the start of the list.
        /// </summary>
        private int depthCounter;

        /// <summary>
        /// A mapping of tabs to their items.
        /// </summary>
        /// <remarks>
        /// There is no guaranteed order. To retrieve ordered tabs, use <see cref="SwitchableTabs"/> or <see cref="AllTabs"/> instead.
        /// </remarks>
        protected IReadOnlyDictionary<T, TabItem<T>> TabMap => tabMap;

        private readonly Dictionary<T, TabItem<T>> tabMap = new Dictionary<T, TabItem<T>>();

        private bool firstSelection = true;

        /// <summary>
        /// Creates a new <see cref="TabControl{T}"/>.
        /// </summary>
        protected TabControl()
        {
            Dropdown = CreateDropdown();

            if (Dropdown != null)
            {
                Dropdown.RelativeSizeAxes = Axes.X;
                Dropdown.Anchor = Anchor.TopRight;
                Dropdown.Origin = Anchor.TopRight;
                Dropdown.Current = Current;

                AddInternal(Dropdown);

                Trace.Assert(Dropdown.Header.Anchor.HasFlagFast(Anchor.x2), $@"The {nameof(Dropdown)} implementation should use a right-based anchor inside a TabControl.");
                Trace.Assert(!Dropdown.Header.RelativeSizeAxes.HasFlagFast(Axes.X), $@"The {nameof(Dropdown)} implementation's header should have a specific size.");
            }

            AddInternal(TabContainer = CreateTabFlow());
            TabContainer.TabVisibilityChanged = updateDropdown;

            if (Dropdown != null)
            {
                // create tab items for already existing items in dropdown (if any).
                foreach (var item in Dropdown.Items)
                    addTab(item, false);
            }

            Current.ValueChanged += _ => firstSelection = false;
        }

        protected override void Update()
        {
            base.Update();

            if (Dropdown != null)
            {
                Dropdown.Header.Height = DrawHeight;
                TabContainer.Padding = new MarginPadding { Right = Dropdown.Header.Width };
            }
        }

        protected override void LoadComplete()
        {
            // Default to first selection in list, if we can
            if (firstSelection && SelectFirstTabByDefault && !Current.Disabled && Items.Any())
                Current.Value = Items.First();

            Current.BindValueChanged(v =>
            {
                if (v.NewValue != null && tabMap.TryGetValue(v.NewValue, out var found))
                    selectTab(found);
                else
                    selectTab(null);
            }, true);

            // TabContainer doesn't have valid layout yet, so TabItems all have y=0 and selectTab() didn't call performTabSort() so we call it here instead
            if (AutoSort && Current.Value != null)
                performTabSort(tabMap[Current.Value]);
        }

        /// <summary>
        /// Pins an item to the start of the <see cref="TabControl{T}"/>.
        /// </summary>
        /// <param name="item">The item to pin.</param>
        public void PinItem(T item)
        {
            if (!tabMap.TryGetValue(item, out TabItem<T> tab))
                return;

            tab.Pinned = true;
        }

        /// <summary>
        /// Unpins an item and returns it to the start of unpinned items.
        /// </summary>
        /// <param name="item">The item to unpin.</param>
        public void UnpinItem(T item)
        {
            if (!tabMap.TryGetValue(item, out TabItem<T> tab))
                return;

            tab.Pinned = false;
        }

        /// <summary>
        /// Adds a new item to the <see cref="TabControl{T}"/>.
        /// </summary>
        /// <param name="item">The item to add.</param>
        public void AddItem(T item) => addTab(item);

        /// <summary>
        /// Removes an item from the <see cref="TabControl{T}"/>.
        /// </summary>
        /// <remarks>
        /// If the current tab is removed and <see cref="SwitchTabOnRemove"/> is <c>true</c>, then selection will change to a new tab if possible or be cleared if there are no tabs remaining in the <see cref="TabControl{T}"/>.
        /// </remarks>
        /// <param name="item">The item to remove.</param>
        public void RemoveItem(T item) => removeTab(item);

        /// <summary>
        /// Removes all items from the <see cref="TabControl{T}"/>.
        /// </summary>
        public void Clear() => Items = Array.Empty<T>();

        private TabItem<T> addTab(T value, bool addToDropdown = true)
        {
            // Do not allow duplicate adding
            if (tabMap.ContainsKey(value))
                throw new InvalidOperationException($"Item {value} has already been added to this {nameof(TabControl<T>)}");

            var tab = CreateTabItem(value);
            AddTabItem(tab, addToDropdown);

            return tab;
        }

        private void removeTab(T value, bool removeFromDropdown = true)
        {
            if (!tabMap.ContainsKey(value))
                throw new InvalidOperationException($"Item {value} doesn't exist in this {nameof(TabControl<T>)}.");

            RemoveTabItem(tabMap[value], removeFromDropdown);
        }

        /// <summary>
        /// Adds an arbitrary <see cref="TabItem{T}"/> to the <see cref="TabControl{T}"/>.
        /// </summary>
        /// <param name="tab">The tab to add.</param>
        /// <param name="addToDropdown">Whether the tab should be added to the <see cref="Dropdown"/> if supported by the <see cref="TabControl{T}"/> implementation.</param>
        protected virtual void AddTabItem(TabItem<T> tab, bool addToDropdown = true)
        {
            tab.PinnedChanged += performTabSort;
            tab.ActivationRequested += activationRequested;

            items.Add(tab.Value);
            tabMap[tab.Value] = tab;

            if (addToDropdown)
                Dropdown?.AddDropdownItem(tab.Value);

            TabContainer.Add(tab);
        }

        /// <summary>
        /// Removes a <see cref="TabItem{T}"/> from this <see cref="TabControl{T}"/>.
        /// </summary>
        /// <remarks>
        /// If the current tab is removed and <see cref="SwitchTabOnRemove"/> is <c>true</c>, then selection will change to a new tab if possible or be cleared if there are no tabs remaining in the <see cref="TabControl{T}"/>.
        /// </remarks>
        /// <param name="tab">The tab to remove.</param>
        /// <param name="removeFromDropdown">Whether the tab should be removed from the <see cref="Dropdown"/> if supported by the <see cref="TabControl{T}"/> implementation.</param>
        protected virtual void RemoveTabItem(TabItem<T> tab, bool removeFromDropdown = true)
        {
            if (!tab.IsRemovable)
                throw new InvalidOperationException($"Cannot remove non-removable tab {tab}. Ensure {nameof(TabItem.IsRemovable)} is set appropriately.");

            if (SwitchTabOnRemove && tab == SelectedTab)
            {
                if (SwitchableTabs.Count() < 2)
                    SelectedTab = null;
                else
                {
                    // check all tabs as to include self (in correct iteration order)
                    bool anySwitchableTabsToRight = AllTabs.SkipWhile(t => t != tab).Skip(1).Any(t => t.IsSwitchable);
                    SwitchTab(anySwitchableTabsToRight ? 1 : -1);
                }
            }

            items.Remove(tab.Value);
            tabMap.Remove(tab.Value);

            if (removeFromDropdown)
                Dropdown?.RemoveDropdownItem(tab.Value);

            TabContainer.Remove(tab);
        }

        /// <summary>
        /// Callback on the change of visibility of a tab.
        /// Used to update the item's status in the overflow dropdown if required.
        /// </summary>
        private void updateDropdown(TabItem<T> tab, bool isVisible)
        {
            if (isVisible)
                Dropdown?.HideItem(tab.Value);
            else
                Dropdown?.ShowItem(tab.Value);
        }

        /// <summary>
        /// Selects a <see cref="TabItem{T}"/>.
        /// </summary>
        /// <param name="tab">The tab to select.</param>
        protected virtual void SelectTab(TabItem<T> tab)
        {
            selectTab(tab);
            Current.Value = SelectedTab != null ? SelectedTab.Value : default;
        }

        private void selectTab(TabItem<T> tab)
        {
            // Only reorder if not pinned and not showing
            if (AutoSort && tab != null && !tab.IsPresent && !tab.Pinned)
                performTabSort(tab);

            // Deactivate previously selected tab
            if (SelectedTab != null && SelectedTab != tab) SelectedTab.Active.Value = false;

            SelectedTab = tab;

            if (SelectedTab != null)
                SelectedTab.Active.Value = true;
        }

        /// <summary>
        /// Switches the currently selected tab forward or backward one index, optionally wrapping.
        /// </summary>
        /// <param name="direction">Pass 1 to move to the next tab, or -1 to move to the previous tab.</param>
        /// <param name="wrap">If <c>true</c>, moving past the start or the end of the tab list will wrap to the opposite end.</param>
        public virtual void SwitchTab(int direction, bool wrap = true)
        {
            if (Math.Abs(direction) != 1) throw new ArgumentException("value must be -1 or 1", nameof(direction));

            // the current selected tab may be an non-switchable tab, so search all tabs for a candidate.
            // this is done to ensure ordering (ie. if an non-switchable tab is in the middle).
            var allTabs = TabContainer.AllTabItems.Where(t => t.IsSwitchable || t == SelectedTab);

            if (direction < 0)
                allTabs = allTabs.Reverse();

            var found = allTabs.SkipWhile(t => t != SelectedTab).Skip(1).FirstOrDefault();

            if (found == null && wrap)
                found = allTabs.FirstOrDefault(t => t != SelectedTab);

            if (found != null)
                SelectTab(found);
        }

        private void activationRequested(TabItem<T> tab)
        {
            if (Current.Disabled)
                return;

            SelectTab(tab);
        }

        private void performTabSort(TabItem<T> tab)
        {
            TabContainer.SetLayoutPosition(tab, getTabDepth(tab));

            // IsPresent of TabItems is based on Y position.
            // We reset it here to allow tabs to get a correct initial position.
            tab.Y = 0;
        }

        private float getTabDepth(TabItem<T> tab) => tab.Pinned ? float.MinValue : --depthCounter;

        public bool OnPressed(KeyBindingPressEvent<PlatformAction> e)
        {
            if (IsSwitchable)
            {
                switch (e.Action)
                {
                    case PlatformAction.DocumentNext:
                        SwitchTab(1);
                        return true;

                    case PlatformAction.DocumentPrevious:
                        SwitchTab(-1);
                        return true;
                }
            }

            return false;
        }

        public void OnReleased(KeyBindingReleaseEvent<PlatformAction> e)
        {
        }

        /// <summary>
        /// Creates the <see cref="TabFillFlowContainer"/> to contain the <see cref="TabItem{T}"/>s.
        /// </summary>
        protected virtual TabFillFlowContainer CreateTabFlow() => new TabFillFlowContainer
        {
            Direction = FillDirection.Full,
            RelativeSizeAxes = Axes.Both,
            Depth = -1,
            Masking = true
        };

        public class TabFillFlowContainer : FillFlowContainer<TabItem<T>>
        {
            private bool allowMultiline;

            /// <summary>
            /// Whether tabs should be allowed to flow beyond a single line. If set to false, overflowing tabs will be automatically hidden.
            /// </summary>
            public bool AllowMultiline
            {
                get => allowMultiline;
                set
                {
                    if (value == allowMultiline)
                        return;

                    allowMultiline = value;
                    InvalidateLayout();
                }
            }

            /// <summary>
            /// Gets called whenever the visibility of a tab in this container changes. Gets invoked with the <see cref="TabItem"/> whose visibility changed and the new visibility state (true = visible, false = hidden).
            /// </summary>
            public Action<TabItem<T>, bool> TabVisibilityChanged;

            /// <summary>
            /// The list of tabs currently displayed by this container, in order of appearance.
            /// </summary>
            public IEnumerable<TabItem<T>> TabItems => FlowingChildren.OfType<TabItem<T>>();

            /// <summary>
            /// The list of all tabs in this container, in order of appearance.
            /// </summary>
            public IEnumerable<TabItem<T>> AllTabItems => GetFlowingTabs(AliveInternalChildren).OfType<TabItem<T>>();

            // The flowing children should only contain the present children, but we also need to consider the non-present children for retrieving all tab items.
            // So the ordering is delegated to a separate method (GetFlowingTabs()).
            public sealed override IEnumerable<Drawable> FlowingChildren => GetFlowingTabs(AliveInternalChildren.Where(d => d.IsPresent));

            /// <summary>
            /// Re-orders a given list of <see cref="TabItem{T}"/>s in the order that they should appear.
            /// </summary>
            /// <param name="tabs">The <see cref="TabItem{T}"/>s to order.</param>
            /// <returns>The re-ordered list of <see cref="TabItem{T}"/>s.</returns>
            public virtual IEnumerable<Drawable> GetFlowingTabs(IEnumerable<Drawable> tabs) => tabs.OrderBy(GetLayoutPosition).ThenBy(d => d.ChildID);

            protected override IEnumerable<Vector2> ComputeLayoutPositions()
            {
                foreach (var child in Children)
                    child.Y = 0;

                var result = base.ComputeLayoutPositions().ToArray();
                int i = 0;

                foreach (var child in FlowingChildren.OfType<TabItem<T>>())
                {
                    bool isVisible = allowMultiline || result[i].Y == 0;
                    updateChildIfNeeded(child, isVisible);

                    yield return result[i];

                    i++;
                }
            }

            private readonly Dictionary<TabItem<T>, bool> tabVisibility = new Dictionary<TabItem<T>, bool>();

            private void updateChildIfNeeded(TabItem<T> child, bool isVisible)
            {
                if (!tabVisibility.ContainsKey(child) || tabVisibility[child] != isVisible)
                {
                    TabVisibilityChanged?.Invoke(child, isVisible);
                    tabVisibility[child] = isVisible;

                    if (isVisible)
                        child.Show();
                    else
                        child.Hide();
                }
            }

            public override void Clear(bool disposeChildren)
            {
                tabVisibility.Clear();
                base.Clear(disposeChildren);
            }

            public override bool Remove(TabItem<T> drawable)
            {
                tabVisibility.Remove(drawable);
                return base.Remove(drawable);
            }
        }
    }
}
