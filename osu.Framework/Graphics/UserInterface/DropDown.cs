// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Configuration;
using osu.Framework.Graphics.Containers;

namespace osu.Framework.Graphics.UserInterface
{
    public abstract class DropDown<T> : FillFlowContainer, IBindable
    {
        protected internal DropDownHeader Header;
        protected Menu DropDownMenu;

        protected abstract DropDownHeader CreateHeader();
        protected abstract Menu CreateMenu();

        private readonly List<DropDownMenuItem<T>> selectableItems = new List<DropDownMenuItem<T>>();
        private List<DropDownMenuItem<T>> items = new List<DropDownMenuItem<T>>();
        private readonly Dictionary<T, int> itemDictionary = new Dictionary<T, int>();

        protected IReadOnlyList<DropDownMenuItem<T>> ItemList => items;
        protected IReadOnlyDictionary<T, int> ItemDictionary => itemDictionary;
        protected abstract DropDownMenuItem<T> CreateDropDownItem(string key, T value);

        public IEnumerable<KeyValuePair<string, T>> Items
        {
            get
            {
                return items.Select(i => new KeyValuePair<string, T>(i.DisplayText, i.Value));
            }
            set
            {
                ClearItems();
                if (value == null)
                    return;

                foreach (var entry in value)
                    AddDropDownItem(entry.Key, entry.Value);
            }
        }

        public void AddDropDownItem(string key, T value)
        {
            var item = CreateDropDownItem(key, value);
            item.PositionIndex = items.Count - 1;
            items.Add(item);
            if (item.CanSelect)
            {
                item.Index = selectableItems.Count;
                item.Action = delegate
                {
                    if (DropDownMenu.State == MenuState.Opened)
                        SelectedIndex = item.Index;
                };
                selectableItems.Add(item);
                itemDictionary[item.Value] = item.Index;
            }
            DropDownMenu.ItemsContainer.Add(item);
        }
        // TODO: RemoveDropDownItem?

        public string Description { get; set; }

        private int selectedIndex = -1;

        /// <summary>
        /// Gets the index of the selected item in the menu.
        /// </summary>
        public int SelectedIndex
        {
            get
            {
                return selectedIndex;
            }
            set
            {
                if (SelectedItem != null)
                    SelectedItem.IsSelected = false;

                selectedIndex = value;

                if (SelectedItem != null)
                    SelectedItem.IsSelected = true;

                TriggerValueChanged();
            }
        }

        protected DropDownMenuItem<T> SelectedItem
        {
            get
            {
                if (SelectedIndex < 0)
                    return null;
                return selectableItems[SelectedIndex];
            }
        }

        /// <summary>
        /// Gets the selected item in the menu.
        /// </summary>
        public T SelectedValue
        {
            get
            {
                if (SelectedItem == null)
                    return default(T);
                return SelectedItem.Value;
            }
            set
            {
                Parse(value);
            }
        }

        /// <summary>
        /// Occurs when the selected item changes.
        /// </summary>
        public event EventHandler ValueChanged;

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
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            Header.Label = SelectedItem?.DisplayText;
        }

        public bool Parse(object value)
        {
            if (selectableItems == null)
                return false;

            if (itemDictionary.ContainsKey((T)value))
            {
                SelectedIndex = itemDictionary[(T)value];
                return true;
            }

            return false;
        }

        public void UnbindAll()
        {
            ValueChanged = null;
        }

        public void TriggerValueChanged()
        {
            Header.Label = SelectedItem?.DisplayText;
            DropDownMenu.State = MenuState.Closed;
            ValueChanged?.Invoke(this, null);
        }

        public void ClearItems()
        {
            items.Clear();
            selectableItems.Clear();
            itemDictionary.Clear();
            DropDownMenu.ItemsContainer.Clear();
        }

        internal void HideItem(T val)
        {
            int index;
            if (ItemDictionary.TryGetValue(val, out index))
                ItemList[index]?.Hide();

            updateHeaderVisibility();
        }

        internal void ShowItem(T val)
        {
            int index;
            if (ItemDictionary.TryGetValue(val, out index))
                ItemList[index]?.Show();

            updateHeaderVisibility();
        }

        private void updateHeaderVisibility() => Header.Alpha = ItemList.Any(i => i.IsPresent) ? 1 : 0;
    }
}
