// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;

namespace osu.Framework.Graphics.UserInterface.Tab
{
    public abstract class TabControl<T> : Container
    {
        private int orderCounter;
        private readonly Dictionary<T, TabItem<T>> tabMap;
        private TabDropDownMenu<T> dropDown;
        private TabFillFlowContainer<TabItem<T>> tabs;

        protected IReadOnlyDictionary<T, TabItem<T>> TabMap => tabMap;
        protected TabItem<T> SelectedTab;
        protected abstract TabDropDownMenu<T> CreateDropDownMenu();
        protected abstract TabItem<T> CreateTabItem(T value);

        public T SelectedValue => SelectedTab.Value;
        public IEnumerable<T> Tabs => tabs.Children.Select(tab => tab.Value);

        /// <summary>
        /// Occurs when the selected tab changes.
        /// </summary>
        public event EventHandler<T> ValueChanged;

        /// <summary>
        /// When true, the recently selected tabs are moved to the front of the list
        /// </summary>
        public bool AutoSort { set; get; }

        protected TabControl(float offset = 0)
        {
            AutoSizeAxes = Axes.Y;
            dropDown = CreateDropDownMenu();
            dropDown.ValueChanged += dropDownValueChanged;

            // Create Map of all items
            tabMap = dropDown.Items.ToDictionary(item => item.Value, item =>
            {
                var tab = CreateTabItem(item.Value);
                tab.PinnedChanged += resortTab;
                tab.SelectAction += selectTab;
                return tab;
            });

            Children = new Drawable[]
            {
                tabs = new TabFillFlowContainer<TabItem<T>>
                {
                    Direction = FillDirection.Full,
                    RelativeSizeAxes = Axes.X,
                    Height = dropDown.HeaderHeight,
                    Masking = true,
                    Padding = new MarginPadding
                    {
                        Left = offset,
                        Right = dropDown.HeaderWidth
                    },
                    TabVisibilityChanged = updateDropDown,
                    Children = tabMap.Values
                },
                dropDown
            };
        }

        // Default to first selection in list
        protected override void LoadComplete()
        {
            if (tabs.Children.Any())
                tabs.Children.First().Active = true;
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

        public void AddTab(T value)
        {
            // Do not allow duplicate adding
            if (tabMap.ContainsKey(value))
                return;

            var tab = CreateTabItem(value);
            tab.PinnedChanged += resortTab;
            tab.SelectAction += selectTab;

            tabMap[value] = tab;
            dropDown.AddDropDownItem(value.ToString(), value);
            tabs.Add(tab);
        }

        // Manages the visibility of dropdownitem based on visible tabs
        private void updateDropDown(TabItem<T> tab, bool isVisible)
        {
            if (isVisible)
                dropDown.HideItem(tab.Value);
            else
                dropDown.ShowItem(tab.Value);
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
                tabs.Remove(tab);

            tab.Depth = tab.Pinned ? int.MaxValue : ++orderCounter;

            // IsPresent of TabItems is based on Y position.
            // We reset it here to allow tabs to get a correct initial position.
            tab.Y = 0;

            if (IsLoaded)
                tabs.Add(tab);
        }
    }
}
