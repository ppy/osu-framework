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
        private Dictionary<T, TabItem<T>> tabMap;
        private TabDropDownMenu<T> dropDown;
        private TabFillFlowContainer<TabItem<T>> tabs;
        private TabItem<T> selectedTab;

        protected abstract TabDropDownMenu<T> CreateDropDownMenu();
        protected abstract TabItem<T> CreateTabItem(T value);

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
                    UpdateChild = updateDropDown,
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

        public void AddTab(T value)
        {
            var tab = CreateTabItem(value);
            tab.PinnedChanged += resortTab;
            tab.SelectAction += selectTab;

            if (!tabMap.ContainsKey(value))
            {
                tabMap[value] = tab;
                dropDown.AddDropDownItem(value.ToString(), value);
                tabs.Add(tab);
            }
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
            if (selectedTab != null)
                selectedTab.Active = false;
            selectedTab = tab;
            ValueChanged?.Invoke(this, tab.Value);
        }
    }
}
