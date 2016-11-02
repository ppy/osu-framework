// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
using OpenTK.Graphics;
using osu.Framework.Configuration;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input;
using osu.Framework.Lists;
using System;
using System.Collections.Specialized;

namespace osu.Framework.Graphics.UserInterface
{
    public class DropDownMenu : Container, IBindable
    {
        private bool opened;

        protected Container ComboBox;
        protected Box ComboBoxBackground;
        protected virtual Color4 ComboBoxBackgroundColour => Color4.DarkGray;
        protected virtual Color4 ComboBoxBackgroundColourHover => Color4.Gray;
        protected Container ComboBoxForeground;
        protected SpriteText ComboBoxLabel;
        protected Container ComboBoxCaret;

        protected Container DropDownList;
        protected virtual float DropDownListSpacing => 0;

        protected virtual Type MenuItemType => typeof(DropDownMenuItem);
        protected virtual Type MenuHeaderType => typeof(DropDownMenuHeader);

        public bool NeedsRefresh { get; protected set; }

        public string Description { get; set; }

        private CollectionView items;

        /// <summary>
        /// Collection of items contained in this menu.
        /// </summary>
        public CollectionView Items
        {
            get
            {
                return items;
            }
            set
            {
                if (items != null)
                {
                    items.CollectionChanged -= itemsCollectionChanged;
                    items.ValueChanged -= itemsValueChanged;
                }
                    
                items = value;

                if (items != null)
                {
                    items.CollectionChanged += itemsCollectionChanged;
                    items.ValueChanged += itemsValueChanged;
                }
                    
                TriggerListChanged();
            }
        }

        /// <summary>
        /// Gets the index of the selected item in the menu.
        /// </summary>
        public int SelectedIndex
        {
            get
            {
                return (Items != null) ? Items.CurrentIndex : 0;
            }
            set
            {
                if (Items != null)
                    Items.CurrentIndex = value;
            }
        }

        /// <summary>
        /// Gets the selected item in the menu.
        /// </summary>
        public object SelectedItem
        {
            get
            {
                return Items?.CurrentItem;
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

        public DropDownMenu()
        {
            items = new CollectionView();

            Children = new Drawable[]
            {
                ComboBox = new Container
                {
                    Masking = true,
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Width = 1,
                    Children = new Drawable[]
                    {
                        ComboBoxBackground = new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                        },
                        ComboBoxForeground = new Container
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Children = new Drawable[]
                            {
                                ComboBoxLabel = new SpriteText
                                {
                                    Text = @"",
                                },
                                ComboBoxCaret = new SpriteText
                                {
                                    Anchor = Anchor.TopRight,
                                    Origin = Anchor.TopRight,
                                    Text = @"+",
                                },
                            }
                        },
                    }
                },
                DropDownList = new Container
                {
                    
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Width = 1,
                    Alpha = 0,
                },
            };
        }

        public override void Load(BaseGame game)
        {
            base.Load(game);
            AutoSizeAxes = Axes.Y;
            ComboBoxBackground.Colour = ComboBoxBackgroundColour;
            ComboBoxLabel.Text = SelectedItem?.ToString();
        }

        public void Open()
        {
            opened = true;
            if (NeedsRefresh)
                refreshDropDownList();
            AnimateOpen();
        }

        public void Close()
        {
            opened = false;
            AnimateClose();
        }

        public bool Parse(object s)
        {
            if (Items == null)
                return false;
            return Items.Parse(s);
        }

        public void UnbindAll()
        {
            ValueChanged = null;
        }

        public void TriggerValueChanged()
        {
            ComboBoxLabel.Text = GetItemLabel(SelectedItem);
            NeedsRefresh = true;
            ValueChanged?.Invoke(this, null);
        }

        public void TriggerListChanged()
        {
            NeedsRefresh = true;
            if (DropDownList != null && DropDownList.IsLoaded)
            {
                opened = false;
                AnimateClose();
            }
        }

        protected virtual string GetItemLabel(object item)
        {
            return item?.ToString();
        }

        protected virtual void AnimateOpen()
        {
            foreach (Drawable child in DropDownList.Children)
            {
                child.MoveToY((child as DropDownMenuItem).ExpectedPositionY);
                child.Show();
            }
            DropDownList.Show();
        }

        protected virtual void AnimateClose()
        {
            foreach (Drawable child in DropDownList.Children)
            {
                child.MoveToY(0);
                child.Hide();
            }
            DropDownList.Hide();
        }

        protected override bool OnClick(InputState state)
        {
            if (opened)
                Close();
            else
                Open();
            return true;
        }

        protected override bool OnHover(InputState state)
        {
            ComboBoxBackground.Colour = ComboBoxBackgroundColourHover;
            return base.OnHover(state);
        }

        protected override void OnHoverLost(InputState state)
        {
            ComboBoxBackground.Colour = ComboBoxBackgroundColour;
            base.OnHoverLost(state);
        }

        private void itemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) => TriggerListChanged();

        private void itemsValueChanged(object sender, EventArgs e) => TriggerValueChanged();

        private void addHeader(int itemIndex, int positionIndex, int level)
        {
            DropDownMenuHeader newHeader = (DropDownMenuHeader)Activator.CreateInstance(MenuHeaderType, new[] { this });
            newHeader.Item = Items.GroupDescriptions[level].GetPropertyStringValue(Items[itemIndex]);
            newHeader.Index = -1;
            newHeader.PositionIndex = positionIndex;
            newHeader.Depth = -positionIndex;
            newHeader.Level = level;
            newHeader.Alpha = 0;

            DropDownList.Add(newHeader);
        }

        private void addItem(int itemIndex, int positionIndex)
        {
            DropDownMenuItem newItem = (DropDownMenuItem)Activator.CreateInstance(MenuItemType, new[] { this });
            newItem.Item = Items[itemIndex];
            newItem.Index = itemIndex;
            newItem.PositionIndex = positionIndex;
            newItem.Depth = -positionIndex;
            newItem.Alpha = 0;

            DropDownList.Add(newItem);
        }

        private void refreshDropDownList()
        {
            if (DropDownList == null || !DropDownList.IsLoaded)
                return;

            DropDownList.Clear();
            DropDownList.Position = new Vector2(0, ComboBox.Height + DropDownListSpacing);
            for (int itemIndex = 0, positionIndex = 0; itemIndex < Items.Count; itemIndex++, positionIndex++)
            {
                for (int level = Items.PropertiesEqualToPreviousItem(itemIndex); level < Items.GroupDescriptions?.Count; level++)
                {
                    addHeader(itemIndex, positionIndex, level);
                    positionIndex++;
                }

                addItem(itemIndex, positionIndex);
            }

            NeedsRefresh = false;
        }
    }
}
