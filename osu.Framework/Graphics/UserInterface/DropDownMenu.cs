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

        private ViewCollection<object> items;
        public ViewCollection<object> Items
        {
            get
            {
                return items;
            }
            set
            {
                if (items != null)
                    items.CollectionChanged -= itemsCollectionChanged;
                items = value;
                if (items != null)
                    items.CollectionChanged += itemsCollectionChanged;
                TriggerListChanged();
            }
        }

        private int selectedIndex;
        public int SelectedIndex
        {
            get
            {
                return selectedIndex;
            }
            set
            {
                selectedIndex = Math.Max(0, Math.Min(value, Items.Count));
                TriggerValueChanged();
            }
        }

        public object SelectedItem
        {
            get
            {
                if (Items.Count == 0)
                    return null;
                return Items[SelectedIndex];
            }
            set
            {
                Parse(value);
            }
        }

        public event EventHandler ValueChanged;

        public DropDownMenu()
        {
            items = new ViewCollection<object>();

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

        public bool Parse(object s)
        {
            if (s == null) return false;

            for (int i = 0; i < Items.Count; i++)
            {
                if (s.Equals(Items[i]))
                {
                    SelectedIndex = i;
                    return true;
                }
            }

            return false;
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
            {
                opened = false;
                AnimateClose();
            }
            else
            {
                opened = true;
                if (NeedsRefresh)
                    refreshDropDownList();
                AnimateOpen();
            }
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

        private void itemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // Items resetted
            if (e.Action == NotifyCollectionChangedAction.Reset)
                SelectedIndex = 0;

            // Items replaced, SelectedItem was among those; notify value change
            if (e.Action == NotifyCollectionChangedAction.Replace &&
                e.NewStartingIndex <= SelectedIndex && SelectedIndex < e.OldStartingIndex + e.NewItems.Count)
                TriggerValueChanged();

            // Items added before SelectedItem; move forward index
            if (e.Action == NotifyCollectionChangedAction.Add && e.NewStartingIndex <= SelectedIndex)
                SelectedIndex += e.NewItems.Count;

            // Items removed before SelectedItem
            if (e.Action == NotifyCollectionChangedAction.Remove && e.OldStartingIndex <= SelectedIndex)
            {
                // SelectedItem was among those; auto select item previous to removed items
                if (SelectedIndex < e.OldStartingIndex + e.OldItems.Count)
                    SelectedIndex = Math.Max(0, e.OldStartingIndex - 1);
                // Otherwise move back index
                else
                    SelectedIndex -= e.OldItems.Count;
            }

            // Items moved
            if (e.Action == NotifyCollectionChangedAction.Move)
            {
                // Items removed before SelectedItem
                if (e.OldStartingIndex <= SelectedIndex)
                {
                    // Selected item was among moved items; calculate new position
                    if (SelectedIndex < e.OldStartingIndex + e.OldItems.Count)
                        SelectedIndex = SelectedIndex - e.OldStartingIndex + e.NewStartingIndex;
                    // Items removed after SelectedItem; move back index
                    else if (SelectedIndex < e.NewStartingIndex)
                        SelectedIndex -= e.OldItems.Count;
                }
                // Items added before SelectedItem; move forward index
                else if (e.NewStartingIndex <= SelectedIndex)
                    SelectedIndex += e.OldItems.Count;
            }

            TriggerListChanged();
        }

        private void refreshDropDownList()
        {
            if (DropDownList == null || !DropDownList.IsLoaded)
                return;

            DropDownList.Clear();
            DropDownList.Position = new Vector2(0, ComboBox.Height + DropDownListSpacing);
            for (int i = 0, j = 0; i < Items.Count; i++, j++)
            {
                for (int eq = Items.PropertiesEqualToPreviousItem(i); eq < Items.GroupDescriptions?.Count; eq++)
                {
                    DropDownMenuHeader newHeader = (DropDownMenuHeader)Activator.CreateInstance(MenuHeaderType, new[] { this });
                    newHeader.Item = Items[i].GetType().GetProperty(Items.GroupDescriptions[eq].PropertyName).GetValue(Items[i]);
                    newHeader.Index = -1;
                    newHeader.PositionIndex = j;
                    newHeader.Depth = -j;
                    newHeader.Level = eq;
                    newHeader.Alpha = 0;

                    DropDownList.Add(newHeader);
                    j++;
                }

                DropDownMenuItem newItem = (DropDownMenuItem)Activator.CreateInstance(MenuItemType, new[] { this });
                newItem.Item = Items[i];
                newItem.Index = i;
                newItem.PositionIndex = j;
                newItem.Depth = -j;
                newItem.Alpha = 0;

                DropDownList.Add(newItem);
            }

            NeedsRefresh = false;
        }
    }
}
