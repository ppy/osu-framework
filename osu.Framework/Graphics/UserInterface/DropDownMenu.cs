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
using osu.Framework.Input;

namespace osu.Framework.Graphics.UserInterface
{
    public abstract class DropDownMenu<T> : FlowContainer, IBindable, IStateful<DropDownMenuState>
    {
        private bool listInitialized;

        private readonly List<DropDownMenuItem<T>> selectableItems = new List<DropDownMenuItem<T>>();
        private List<DropDownMenuItem<T>> items = new List<DropDownMenuItem<T>>();
        private readonly Dictionary<T, int> itemDictionary = new Dictionary<T, int>();

        protected abstract IEnumerable<DropDownMenuItem<T>> GetDropDownItems(IEnumerable<KeyValuePair<string, T>> values);

        public IEnumerable<KeyValuePair<string, T>> Items
        {
            get
            {
                return items.Select(i => new KeyValuePair<string, T>(i.DisplayText, i.Value));
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
                            if (State == DropDownMenuState.Opened)
                                SelectedIndex = item.Index;
                        };
                        selectableItems.Add(item);
                        itemDictionary[item.Value] = item.Index;
                    }
                }
            }
        }

        protected DropDownHeader Header;

        protected Container ContentContainer;
        protected FlowContainer<DropDownMenuItem<T>> DropDownItemsContainer;

        protected abstract DropDownHeader CreateHeader();

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
                UpdateContentHeight();
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

        private DropDownMenuState state = DropDownMenuState.Closed;
        protected Box ContentBackground;

        public DropDownMenuState State
        {
            get
            {
                return state;
            }
            set
            {
                if (state == value) return;
                state = value;

                switch (value)
                {
                    case DropDownMenuState.Closed:
                        TriggerFocusLost();
                        AnimateClose();
                        break;
                    case DropDownMenuState.Opened:
                        TriggerFocus();
                        if (!listInitialized)
                            initializeDropDownList();
                        AnimateOpen();
                        break;
                }

                UpdateContentHeight();
            }
        }

        /// <summary>
        /// Occurs when the selected item changes.
        /// </summary>
        public event EventHandler ValueChanged;

        public DropDownMenu()
        {
            AutoSizeAxes = Axes.Y;
            Direction = FlowDirection.VerticalOnly;

            Children = new Drawable[]
            {
                Header = CreateHeader(),
                ContentContainer = new Container
                {
                    RelativeSizeAxes = Axes.X,
                    Masking = true,
                    Children = new Drawable[]
                    {
                        ContentBackground = new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = Color4.Black,
                        },
                        new ScrollContainer
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

            Header.Action = Toggle;

            DropDownItemsContainer.OnAutoSize += UpdateContentHeight;
        }

        protected virtual void UpdateContentHeight()
        {
            ContentContainer.Height = ContentHeight;
        }

        protected override bool OnFocus(InputState state) => true;

        protected override void OnFocusLost(InputState state) => State = DropDownMenuState.Closed;

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
            State = DropDownMenuState.Closed;
            ValueChanged?.Invoke(this, null);
        }

        protected virtual void AnimateOpen()
        {
            foreach (DropDownMenuItem<T> child in DropDownItemsContainer.Children)
                child.Show();
            ContentContainer.Show();
        }

        protected virtual void AnimateClose()
        {
            foreach (DropDownMenuItem<T> child in DropDownItemsContainer.Children)
                child.Hide();
            ContentContainer.Hide();
        }

        public void Toggle() => State = State == DropDownMenuState.Closed ? DropDownMenuState.Opened : DropDownMenuState.Closed;

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

            listInitialized = true;
        }

        protected float ContentHeight => Math.Min(DropDownItemsContainer.Height, MaxDropDownHeight);
    }

    public enum DropDownMenuState
    {
        Closed,
        Opened,
    }
}