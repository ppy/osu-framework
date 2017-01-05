// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
using OpenTK.Graphics;
using osu.Framework.Configuration;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using System;
using System.Collections.Generic;
using System.Linq;

namespace osu.Framework.Graphics.UserInterface
{
    public enum DropDownMenuState
    {
        Closed,
        Opened,
    }

    public abstract class DropDownMenu<T> : Container, IBindable, IStateful<DropDownMenuState>
    {
        private bool opened;
        private bool listInitialized = false;

        private readonly List<DropDownMenuItem<T>> selectableItems = new List<DropDownMenuItem<T>>();
        private List<DropDownMenuItem<T>> items = new List<DropDownMenuItem<T>>();
        private readonly Dictionary<T, int> itemDictionary = new Dictionary<T, int>();

        protected abstract IEnumerable<DropDownMenuItem<T>> GetDropDownItems(IEnumerable<T> values);

        public IEnumerable<T> Items
        {
            get
            {
                return items.Select(i => i.Value);
            }
            set
            {
                listInitialized = false;
                ClearItems();
                foreach (var item in GetDropDownItems(value))
                {
                    item.PositionIndex = items.Count - 1;
                    items.Add(item);
                    if (item.CanSelect)
                    {
                        item.Index = selectableItems.Count;
                        item.Action = delegate
                        {
                            if (opened)
                                SelectedIndex = item.Index;
                        };
                        selectableItems.Add(item);
                        itemDictionary[item.Value] = item.Index;
                    }
                }
            }
        }

        protected DropDownComboBox ComboBox;

        protected Container DropDown;
        protected ScrollContainer DropDownScroll;
        protected FlowContainer<DropDownMenuItem<T>> DropDownItemsContainer;
        protected Box DropDownBackground;
        protected virtual float DropDownListSpacing => 0;

        protected abstract DropDownComboBox CreateComboBox();

        private int maxDropDownHeight = 100;

        /// <summary>
        /// Maximum height the Drop-down box can reach.
        /// </summary>
        public int MaxDropDownHeight
        {
            get
            {
                return maxDropDownHeight;
            }
            set
            {
                maxDropDownHeight = value;
                updateDropDownListSize();
            }
        }

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

        public DropDownMenuState State
        {
            get
            {
                return opened ? DropDownMenuState.Opened : DropDownMenuState.Closed;
            }
            set
            {
                switch (value)
                {
                    case DropDownMenuState.Closed:
                        Close();
                        break;
                    case DropDownMenuState.Opened:
                        Open();
                        break;
                }
            }
        }

        /// <summary>
        /// Occurs when the selected item changes.
        /// </summary>
        public event EventHandler ValueChanged;

        public DropDownMenu()
        {
            AutoSizeAxes = Axes.Y;

            Children = new Drawable[]
            {
                ComboBox = CreateComboBox(),
                DropDown = new Container
                {
                    RelativeSizeAxes = Axes.X,
                    Masking = true,
                    Children = new Drawable[]
                    {
                        DropDownBackground = new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = Color4.Black,
                            Alpha = 0.8f,
                        },
                        DropDownScroll = new ScrollContainer
                        {
                            Masking = false,
                            Children = new Drawable[]
                            {
                                DropDownItemsContainer = new FlowContainer<DropDownMenuItem<T>>
                                {
                                    RelativeSizeAxes = Axes.X,
                                    AutoSizeAxes = Axes.Y,
                                    Direction = FlowDirection.VerticalOnly,
                                },
                            },
                        },
                    },
                }
            };

            ComboBox.Action = Toggle;
            ComboBox.CloseAction = Close;

            DropDownItemsContainer.OnAutoSize += updateDropDownListSize;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            ComboBox.Label = SelectedItem?.DisplayText;
        }

        public void Open()
        {
            if (!IsLoaded)
                return;

            opened = true;
            if (!listInitialized)
                initializeDropDownList();
            AnimateOpen();
        }

        public void Close()
        {
            opened = false;
            if (IsLoaded)
                AnimateClose();
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
            ComboBox.Label = SelectedItem?.DisplayText;
            Close();
            ValueChanged?.Invoke(this, null);
        }

        protected virtual void AnimateOpen()
        {
            foreach (DropDownMenuItem<T> child in DropDownItemsContainer.Children)
                child.Show();
            DropDown.Show();
        }

        protected virtual void AnimateClose()
        {
            foreach (DropDownMenuItem<T> child in DropDownItemsContainer.Children)
                child.Hide();
            DropDown.Hide();
        }

        public void Toggle()
        {
            if (opened)
                Close();
            else
                Open();
        }

        public void ClearItems()
        {
            items.Clear();
            selectableItems.Clear();
            itemDictionary.Clear();
            DropDownItemsContainer.Clear();
        }

        private void initializeDropDownList()
        {
            if (DropDownItemsContainer == null || !DropDownItemsContainer.IsLoaded)
                return;

            for (int i = 0; i < items.Count; i++)
                DropDownItemsContainer.Add(items[i]);

            DropDown.Position = new Vector2(0, ComboBox.Height + DropDownListSpacing);

            listInitialized = true;
        }

        private void updateDropDownListSize()
        {
            DropDown.Height = Math.Min(DropDownItemsContainer.Height, MaxDropDownHeight);
        }
    }
}