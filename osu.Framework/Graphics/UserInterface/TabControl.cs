﻿// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using osu.Framework.Configuration;
using osu.Framework.Extensions;
using osu.Framework.Graphics.Containers;
using OpenTK;

namespace osu.Framework.Graphics.UserInterface
{
    /// <summary>
    /// A single-row control to display a list of selectable tabs along with an optional right-aligned dropdown
    /// containing overflow items (tabs which cannot be displayed in the allocated width). Includes
    /// support for pinning items, causing them to be displayed before all other items at the
    /// start of the list.
    /// </summary>
    /// <typeparam name="T">The type of item to be represented by tabs.</typeparam>
    public abstract class TabControl<T> : Container, IHasCurrentValue<T>
    {
        public Bindable<T> Current { get; } = new Bindable<T>();

        /// <summary>
        /// A list of items currently in the tab control in the other they are dispalyed.
        /// </summary>
        public IEnumerable<T> Items => TabContainer.Children.Select(tab => tab.Value);

        /// <summary>
        /// When true, tabs selected from the overflow dropdown will be moved to the front of the list (after pinned items).
        /// </summary>
        public bool AutoSort { set; get; }

        /// <summary>
        /// We need a special case here to allow for the dropdown "overflowing" our own bounds.
        /// </summary>
        protected override bool InternalContains(Vector2 screenSpacePos) => base.InternalContains(screenSpacePos) || (Dropdown?.Contains(screenSpacePos) ?? false);

        protected Dropdown<T> Dropdown;

        protected TabFillFlowContainer<TabItem<T>> TabContainer;

        protected IReadOnlyDictionary<T, TabItem<T>> TabMap => tabMap;

        protected TabItem<T> SelectedTab;

        /// <summary>
        /// Creates an optional overflow dropdown.
        /// When implementing this dropdown make sure:
        ///  - It is made to be anchored to the right-hand side of its parent.
        ///  - The dropdown's header does *not* have a relative x axis.
        /// </summary>
        protected abstract Dropdown<T> CreateDropdown();

        /// <summary>
        /// Creates a tab item.
        /// </summary>
        protected abstract TabItem<T> CreateTabItem(T value);

        /// <summary>
        /// Incremented each time a tab needs to be inserted at the start of the list.
        /// </summary>
        private int depthCounter;

        /// <summary>
        /// A mapping of tabs to their items.
        /// </summary>
        private readonly Dictionary<T, TabItem<T>> tabMap;

        protected TabControl()
        {
            Dropdown = CreateDropdown();
            if (Dropdown != null)
            {
                Dropdown.RelativeSizeAxes = Axes.X;
                Dropdown.Anchor = Anchor.TopRight;
                Dropdown.Origin = Anchor.TopRight;
                Dropdown.Current.BindTo(Current);

                Add(Dropdown);

                Trace.Assert((Dropdown.Header.Anchor & Anchor.x2) > 0, $@"The {nameof(Dropdown)} implementation should use a right-based anchor inside a TabControl.");
                Trace.Assert((Dropdown.Header.RelativeSizeAxes & Axes.X) == 0, $@"The {nameof(Dropdown)} implementation's header should have a specific size.");

                // create tab items for already existing items in dropdown (if any).
                tabMap = Dropdown.Items.ToDictionary(item => item.Value, item => addTab(item.Value, false));
            }
            else
                tabMap = new Dictionary<T, TabItem<T>>();

            Add(TabContainer = new TabFillFlowContainer<TabItem<T>>
            {
                Direction = FillDirection.Full,
                RelativeSizeAxes = Axes.Both,
                Depth = -1,
                Masking = true,
                TabVisibilityChanged = updateDropdown,
                Children = tabMap.Values
            });

            Current.ValueChanged += newSelection =>
            {
                if (IsLoaded)
                    selectTab(tabMap[Current]);
                else
                    //will be handled in LoadComplete
                    SelectedTab = tabMap[Current];
            };
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

        // Default to first selection in list
        protected override void LoadComplete()
        {
            if (SelectedTab != null)
                selectTab(SelectedTab);
            else if (TabContainer.Children.Any())
                TabContainer.Children.First().Active = true;
        }

        /// <summary>
        /// Pin an item to the start of the list.
        /// </summary>
        /// <param name="item">The item to pin.</param>
        public void PinItem(T item)
        {
            TabItem<T> tab;
            if (!tabMap.TryGetValue(item, out tab))
                return;
            tab.Pinned = true;
        }

        /// <summary>
        /// Unpin an item and return it to the start of unpinned items.
        /// </summary>
        /// <param name="item">The item to unpin.</param>
        public void UnpinItem(T item)
        {
            TabItem<T> tab;
            if (!tabMap.TryGetValue(item, out tab))
                return;
            tab.Pinned = false;
        }

        /// <summary>
        /// Add a new item to the control.
        /// </summary>
        /// <param name="item">The item to add.</param>
        public void AddItem(T item) => addTab(item);

        private TabItem<T> addTab(T value, bool addToDropdown = true)
        {
            // Do not allow duplicate adding
            if (tabMap.ContainsKey(value))
                return null;

            var tab = CreateTabItem(value);
            AddTabItem(tab, addToDropdown);

            return tab;
        }

        /// <summary>
        /// Adds an arbitrary <see cref="TabItem{T}"/> to the control
        /// </summary>
        /// <param name="tab">The tab to add</param>
        /// <param name="addToDropdown">Whether the tab should be added to the Dropdown if supported by the <see cref="TabControl{T}"/> implementation</param>
        protected void AddTabItem(TabItem<T> tab, bool addToDropdown = true)
        {
            tab.PinnedChanged += resortTab;
            tab.SelectAction += selectTab;

            tabMap[tab.Value] = tab;
            if (addToDropdown)
                Dropdown?.AddDropdownItem((tab.Value as Enum)?.GetDescription() ?? tab.Value.ToString(), tab.Value);
            TabContainer.Add(tab);
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

        private void selectTab(TabItem<T> tab)
        {
            // Only reorder if not pinned and not showing
            if (AutoSort && !tab.IsPresent && !tab.Pinned)
                resortTab(tab);

            // Deactivate previously selected tab
            if (SelectedTab != null && SelectedTab != tab) SelectedTab.Active = false;

            SelectedTab = tab;
            SelectedTab.Active = true;

            Current.Value = SelectedTab.Value;
        }

        private void resortTab(TabItem<T> tab)
        {
            if (IsLoaded)
                TabContainer.Remove(tab);

            tab.Depth = tab.Pinned ? float.MaxValue : ++depthCounter;

            // IsPresent of TabItems is based on Y position.
            // We reset it here to allow tabs to get a correct initial position.
            tab.Y = 0;

            if (IsLoaded)
                TabContainer.Add(tab);
        }

        public class TabFillFlowContainer<U> : FillFlowContainer<U> where U : TabItem
        {
            public Action<U, bool> TabVisibilityChanged;

            protected override IComparer<Drawable> DepthComparer => new ReverseCreationOrderDepthComparer();

            protected override IEnumerable<Drawable> FlowingChildren => base.FlowingChildren.Reverse();

            protected override IEnumerable<Vector2> ComputeLayoutPositions()
            {
                foreach (var child in Children)
                    child.Y = 0;

                var result = base.ComputeLayoutPositions().ToArray();
                int i = 0;
                foreach (var child in FlowingChildren.OfType<U>())
                {
                    updateChildIfNeeded(child, result[i].Y == 0);
                    ++i;
                }
                return result;
            }

            private readonly Dictionary<U, bool> tabVisibility = new Dictionary<U, bool>();

            private void updateChildIfNeeded(U child, bool isVisible)
            {
                if (!tabVisibility.ContainsKey(child) || tabVisibility[child] != isVisible)
                {
                    TabVisibilityChanged?.Invoke(child, isVisible);
                    tabVisibility[child] = isVisible;
                }
            }
        }
    }
}
