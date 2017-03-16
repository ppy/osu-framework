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
    public abstract class TabControl<T> : Container
    {
        private int orderCounter;
        private readonly Dictionary<T, TabItem<T>> tabMap;
        protected DropDownMenu<T> DropDown;
        protected TabFillFlowContainer<TabItem<T>> TabContainer;

        protected IReadOnlyDictionary<T, TabItem<T>> TabMap => tabMap;
        protected TabItem<T> SelectedTab;
        protected abstract DropDownMenu<T> CreateDropDownMenu();
        protected abstract TabItem<T> CreateTabItem(T value);

        public T SelectedValue => SelectedTab.Value;
        public IEnumerable<T> Tabs => TabContainer.Children.Select(tab => tab.Value);

        protected override bool InternalContains(Vector2 screenSpacePos) => base.InternalContains(screenSpacePos) || DropDown.Contains(screenSpacePos);

        /// <summary>
        /// Occurs when the selected tab changes.
        /// </summary>
        public event EventHandler<T> ValueChanged;

        /// <summary>
        /// When true, the recently selected tabs are moved to the front of the list
        /// </summary>
        public bool AutoSort { set; get; }

        protected TabControl()
        {
            DropDown = CreateDropDownMenu();
            DropDown.RelativeSizeAxes = Axes.X;
            DropDown.Anchor = Anchor.TopRight;
            DropDown.Origin = Anchor.TopRight;
            DropDown.ValueChanged += dropDownValueChanged;

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

        public void PinTab(T value)
        {
            TabItem<T> tab;
            if (!tabMap.TryGetValue(value, out tab))
                return;
            tab.Pinned = true;
        }

        public void UnpinTab(T value)
        {
            TabItem<T> tab;
            if (!tabMap.TryGetValue(value, out tab))
                return;
            tab.Pinned = false;
        }

        public void AddTab(T value) => addTab(value);

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

        // Manages the visibility of dropdownitem based on visible tabs
        private void updateDropDown(TabItem<T> tab, bool isVisible)
        {
            if (isVisible)
                DropDown.HideItem(tab.Value);
            else
                DropDown.ShowItem(tab.Value);
        }

        private void dropDownValueChanged(object sender, EventArgs eventArgs)
        {
            var dropDownMenu = sender as DropDownMenu<T>;
            if (dropDownMenu != null)
            {
                TabItem<T> tab;
                if (tabMap.TryGetValue(dropDownMenu.SelectedValue, out tab))
                    tab.Active = true;
            }
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
            ValueChanged?.Invoke(this, tab.Value);
        }

        private void resortTab(TabItem<T> tab)
        {
            if (IsLoaded)
                TabContainer.Remove(tab);

            tab.Depth = tab.Pinned ? float.MaxValue : ++orderCounter;

            // IsPresent of TabItems is based on Y position.
            // We reset it here to allow tabs to get a correct initial position.
            tab.Y = 0;

            if (IsLoaded)
                TabContainer.Add(tab);
        }
    }
}
