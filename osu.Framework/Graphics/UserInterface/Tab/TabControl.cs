// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using osu.Framework.Graphics.Containers;

namespace osu.Framework.Graphics.UserInterface.Tab
{
    public abstract class TabControl<T> : Container
    {
        public event EventHandler<T> ValueChanged;

        protected abstract TabDropDownMenu<T> CreateDropDownMenu();
        protected abstract TabItem<T> CreateTabItem(T value);

        private TabDropDownMenu<T> dropDown;
        private FillFlowContainer<TabItem<T>> pinnedTabs;
        private FillFlowContainer<TabItem<T>> visibleTabs;
        private TabItem<T> selectedTab;

        private float visibleMaxWidth => pinnedMaxWidth - pinnedTabs.Width;
        private float pinnedMaxWidth => Width - dropDown.HeaderWidth - Prefix.Width;

        public FillFlowContainer Prefix { get; }

        protected TabControl()
        {
            AutoSizeAxes = Axes.Y;

            dropDown = CreateDropDownMenu();
            dropDown.ValueChanged += dropDownValueChanged;

            // Populate visible with as many items as fit
            var items = dropDown.Items.ToList();
            /*var allTabs = new List<TabItem<T>>();
            for (int i = items.Count - 1; i >= 10; i--) {
                var tab = CreateTabItem(items[i].Value);
                tab.SelectAction += selectTab;
                dropDown.HideItem(items[i].Value);
                allTabs.Add(tab);
                Debug.WriteLine("PREADDED" + tab);
            }
            visibleTabs.Children = allTabs;*/

            Children = new Drawable[]
            {
                new FillFlowContainer
                {
                    Direction = FillDirection.Horizontal,
                    AutoSizeAxes = Axes.Both,
                    Children = new Drawable[]
                    {
                        Prefix = new FillFlowContainer
                        {
                            Direction = FillDirection.Horizontal,
                            AutoSizeAxes = Axes.Both
                        },
                        pinnedTabs = new FillFlowContainer<TabItem<T>>
                        {
                            Direction = FillDirection.Horizontal,
                            AutoSizeAxes = Axes.Both
                        },
                        visibleTabs = new ReverseFillFlowContainer<TabItem<T>>
                        {
                            Direction = FillDirection.Horizontal,
                            AutoSizeAxes = Axes.Both,
                            // Temp for debugging
                            Children = new TabItem<T>[]
                            {
                                CreateTabItem(items[0].Value),
                                CreateTabItem(items[1].Value)
                            }
                        }
                    }
                },
                dropDown
            };

            Prefix.OnAutoSize += truncateTabs;
            pinnedTabs.OnAutoSize += truncateTabs;
            visibleTabs.OnAutoSize += truncateTabs;
        }

        private void dropDownValueChanged(object sender, EventArgs eventArgs)
        {
            var dropDownMenu = sender as DropDownMenu<T>;
            if (dropDownMenu != null)
                Select(dropDownMenu.SelectedValue);
        }

        // Pins a value so it is always showing at far left
        public void Pin(T tab)
        {
            // Move from visible or hide from dropdown
            var visibleTab = findTab(FindMode.Visible, tab);
            if (visibleTab != null)
                visibleTabs.Remove(visibleTab);
            else
                dropDown.HideItem(tab);

            var pinned = CreateTabItem(tab);
            pinned.SelectAction += selectTab;
            pinnedTabs.Add(pinned);
        }

        // Select a tab by value
        public void Select(T value)
        {
            var modes = Enum.GetValues(typeof(FindMode)).Cast<FindMode>();
            foreach (var mode in modes)
            {
                TabItem<T> tab = findTab(mode, value);
                if (tab != null)
                {
                    selectTab(tab);
                    tab.SelectAction += selectTab;
                    return;
                }
            }
        }

        // Select a tab by reference
        private void selectTab(TabItem<T> tab)
        {
            // Deactivate previously selected tab
            if (selectedTab != null)
                selectedTab.Active = false;
            tab.Active = true;
            selectedTab = tab;
            ValueChanged?.Invoke(this, tab.Value);
        }

        private TabItem<T> findTab(FindMode mode, T value)
        {
            switch (mode)
            {
                case FindMode.Pinned:
                    return pinnedTabs.Children.FirstOrDefault(c => EqualityComparer<T>.Default.Equals(value, c.Value));
                case FindMode.Visible:
                    return visibleTabs.Children.FirstOrDefault(c => EqualityComparer<T>.Default.Equals(value, c.Value));
                case FindMode.DropDown:
                    return findDropDown(value);
            }
            return null;
        }

        private TabItem<T> findDropDown(T value)
        {
            // Can't select default value
            if (EqualityComparer<T>.Default.Equals(value, default(T)))
                return null;

            var visible = CreateTabItem(value);
            dropDown.HideItem(value);
            visibleTabs.Add(visible);
            return visible;
        }

        // Remove any tabs that cannot fit anymore
        // TODO: Truncate pinned tabs too
        private void truncateTabs()
        {
            float visibleWidth = 0;
            List<TabItem<T>> remove = new List<TabItem<T>>();
            foreach (var child in visibleTabs.Children)
            {
                visibleWidth += child.Width;
                if (visibleWidth > visibleMaxWidth)
                    remove.Add(child);
            }
            // Move value back to dropdown
            foreach (var child in remove)
            {
                dropDown.ShowItem(child.Value);
                Remove(child);
            }
        }

        private enum FindMode
        {
            Pinned,
            Visible,
            DropDown
        }
    }
}
