// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using osu.Framework.Extensions;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using OpenTK;

namespace osu.Framework.Graphics.UserInterface
{
    /// <summary>
    /// A single-row control to display a list of selectable tabs along with a right-aligned dropdown
    /// containing overflow items (tabs which cannot be displayed in the allocated width). Includes
    /// support for pinning items, causing them to be displayed before all other items at the
    /// start of the list.
    /// </summary>
    /// <typeparam name="T">The type of item to be represented by tabs.</typeparam>
    public abstract class TabControl<T> : Container
    {
        /// <summary>
        /// The currently selected item.
        /// </summary>
        public T SelectedItem => SelectedTab.Value;

        /// <summary>
        /// A list of items currently in the tab control in the other they are dispalyed.
        /// </summary>
        public IEnumerable<T> Items => TabContainer.Children.Select(tab => tab.Value);

        /// <summary>
        /// Occurs when the selected tab changes.
        /// </summary>
        public event EventHandler<T> ItemChanged;

        /// <summary>
        /// When true, tabs selected from the overflow dropdown will be moved to the front of the list (after pinned items).
        /// </summary>
        public bool AutoSort { set; get; }

        /// <summary>
        /// We need a special case here to allow for the dropdown "overflowing" our own bounds.
        /// </summary>
        protected override bool InternalContains(Vector2 screenSpacePos) => base.InternalContains(screenSpacePos) || DropDown.Contains(screenSpacePos);

        protected DropDownMenu<T> DropDown;

        protected TabFillFlowContainer<TabItem<T>> TabContainer;

        protected IReadOnlyDictionary<T, TabItem<T>> TabMap => tabMap;

        protected TabItem<T> SelectedTab;

        /// <summary>
        /// Creates the overflow dropdown.
        /// When implementing this dropdown make sure:
        ///  - It is made to be anchored to the right-hand side of its parent.
        ///  - The dropdown's header does *not* have a relative x axis.
        /// </summary>
        protected abstract DropDownMenu<T> CreateDropDownMenu();

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
            DropDown = CreateDropDownMenu();
            DropDown.RelativeSizeAxes = Axes.X;
            DropDown.Anchor = Anchor.TopRight;
            DropDown.Origin = Anchor.TopRight;
            DropDown.ValueChanged += delegate { tabMap[DropDown.SelectedValue].Active = true; };

            Trace.Assert((DropDown.Header.Anchor & Anchor.x2) > 0, $@"The {nameof(DropDown)} implementation should use a right-based anchor inside a TabControl.");
            Trace.Assert((DropDown.Header.RelativeSizeAxes & Axes.X) == 0, $@"The {nameof(DropDown)} implementation's header should have a specific size.");

            // Create Map of all items
            tabMap = DropDown.Items.ToDictionary(item => item.Value, item => addTab(item.Value, false));

            Children = new Drawable[]
            {
                TabContainer = new TabFillFlowContainer<TabItem<T>>
                {
                    Direction = FillDirection.Full,
                    RelativeSizeAxes = Axes.Both,
                    Masking = true,
                    TabVisibilityChanged = updateDropDown,
                    Children = tabMap.Values
                },
                DropDown
            };
        }

        protected override void Update()
        {
            base.Update();

            DropDown.Header.Height = DrawHeight;
            TabContainer.Padding = new MarginPadding
            {
                Right = DropDown.Header.Width
            };
        }

        // Default to first selection in list
        protected override void LoadComplete()
        {
            if (TabContainer.Children.Any())
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
            tab.PinnedChanged += resortTab;
            tab.SelectAction += selectTab;

            tabMap[value] = tab;
            if (addToDropdown)
                DropDown.AddDropDownItem((value as Enum)?.GetDescription() ?? value.ToString(), value);
            TabContainer.Add(tab);

            return tab;
        }

        /// <summary>
        /// Callback on the change of visibility of a tab.
        /// Used to update the item's status in the overflow dropdown if required.
        /// </summary>
        private void updateDropDown(TabItem<T> tab, bool isVisible)
        {
            if (isVisible)
                DropDown.HideItem(tab.Value);
            else
                DropDown.ShowItem(tab.Value);
        }

        private void selectTab(TabItem<T> tab)
        {
            // Only reorder if not pinned and not showing
            if (AutoSort && !tab.IsPresent && !tab.Pinned)
                resortTab(tab);

            // Deactivate previously selected tab
            if (SelectedTab != null)
                SelectedTab.Active = false;
            SelectedTab = tab;
            ItemChanged?.Invoke(this, tab.Value);
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
    }
}
