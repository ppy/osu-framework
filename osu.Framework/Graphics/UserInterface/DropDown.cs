// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Configuration;
using osu.Framework.Graphics.Containers;

namespace osu.Framework.Graphics.UserInterface
{
    public abstract class DropDown<T> : FillFlowContainer
    {
        protected internal DropDownHeader Header;
        protected internal Menu DropDownMenu;

        protected abstract DropDownHeader CreateHeader();
        protected abstract Menu CreateMenu();
        protected abstract DropDownMenuItem<T> CreateMenuItem(string text, T value);

        private readonly Dictionary<T, DropDownMenuItem<T>> itemMap = new Dictionary<T, DropDownMenuItem<T>>();

        protected IEnumerable<DropDownMenuItem<T>> MenuItems => itemMap.Values;

        public IEnumerable<KeyValuePair<string, T>> Items
        {
            get
            {
                return MenuItems.Select(i => new KeyValuePair<string, T>(i.Text, i.Value));
            }
            set
            {
                ClearItems();
                if (value == null)
                    return;

                foreach (var entry in value)
                    AddDropDownItem(entry.Key, entry.Value);

                refreshSelection(null, null);
                if (SelectedItem == null)
                    SelectedItem = MenuItems.FirstOrDefault();
            }
        }

        public void AddDropDownItem(string text, T value)
        {
            var item = CreateMenuItem(text, value);
            item.Action = () =>
            {
                selectedItem = item;
                SelectedValue.Value = item.Value;
                DropDownMenu.State = MenuState.Closed;
            };
            itemMap[item.Value] = item;
            DropDownMenu.ItemsContainer.Add(item);
        }
        // TODO: RemoveDropDownItem?

        public readonly Bindable<T> SelectedValue = new Bindable<T>();

        private DropDownMenuItem<T> selectedItem;
        protected DropDownMenuItem<T> SelectedItem
        {
            get { return selectedItem; }
            set
            {
                selectedItem = value;
                if (value != null)
                    SelectedValue.Value = value.Value;
            }
        }

        protected DropDown()
        {
            AutoSizeAxes = Axes.Y;
            Direction = FillDirection.Vertical;

            Children = new Drawable[]
            {
                Header = CreateHeader(),
                DropDownMenu = CreateMenu()
            };

            Header.Action = DropDownMenu.Toggle;
            SelectedValue.ValueChanged += refreshSelection;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            Header.Label = SelectedItem?.Text;
        }

        private void refreshSelection(object sender, EventArgs e)
        {
            if ((SelectedItem == null || !EqualityComparer<T>.Default.Equals(SelectedItem.Value, SelectedValue.Value))
                && SelectedValue.Value != null)
                itemMap.TryGetValue(SelectedValue.Value, out selectedItem);

            Header.Label = SelectedItem?.Text;
        }

        public void ClearItems()
        {
            itemMap.Clear();
            DropDownMenu.ItemsContainer.Clear();
        }

        internal void HideItem(T val)
        {
            DropDownMenuItem<T> item;
            if (itemMap.TryGetValue(val, out item))
            {
                item.Hide();
                updateHeaderVisibility();
            }
        }

        internal void ShowItem(T val)
        {
            DropDownMenuItem<T> item;
            if (itemMap.TryGetValue(val, out item))
            {
                item.Show();
                updateHeaderVisibility();
            }
        }

        private void updateHeaderVisibility() => Header.Alpha = MenuItems.Any(i => i.IsPresent) ? 1 : 0;
    }
}
