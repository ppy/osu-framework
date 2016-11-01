// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using System;
using osu.Framework.Input;
using OpenTK;
using OpenTK.Graphics;
using osu.Framework.Configuration;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace osu.Framework.Graphics.UserInterface
{
    public class DropDownMenu : Container, IBindable
    {
        protected bool opened;

        protected bool IsListRefreshed;

        protected Container ComboBox;
        protected Box ComboBoxBackgroundBox;
        protected virtual Color4 ComboBoxBackgroundColour => Color4.DarkGray;
        protected virtual Color4 ComboBoxBackgroundColourHover => Color4.Gray;
        protected SpriteText ComboBoxLabel;
        protected Container ComboBoxCaret;
        protected Container DropDownList;
        protected virtual float DropDownListSpacing => 0;

        protected virtual Type MenuItemType => typeof(DropDownMenuItem);

        private string description;

        public string Description
        {
            get { return description; }
            set { description = value; }
        }

        protected override bool OnHover(InputState state)
        {
            ComboBoxBackgroundBox.Colour = ComboBoxBackgroundColourHover;
            return base.OnHover(state);
        }

        protected override void OnHoverLost(InputState state)
        {
            ComboBoxBackgroundBox.Colour = ComboBoxBackgroundColour;
            base.OnHoverLost(state);
        }

        private ObservableCollection<object> items;
        public ObservableCollection<object> Items
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
            items = new ObservableCollection<object>();

            ComboBoxBackgroundBox = new Box
            {
                RelativeSizeAxes = Axes.Both,
                Size = new Vector2(1, 1),
            };

            ComboBoxLabel = new SpriteText
            {
                Text = @"",
            };

            ComboBoxCaret = new SpriteText
            {
                Anchor = Anchor.TopRight,
                Origin = Anchor.TopRight,
                Text = @"+",
            };

            ComboBox = new Container
            {
                Masking = true,
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Width = 1,
                Children = new Drawable[]
                {
                    ComboBoxBackgroundBox,
                    ComboBoxLabel,
                    ComboBoxCaret,
                }
            };

            DropDownList = new Container
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Width = 1,
                Alpha = 0,
            };

            Children = new Drawable[]
            {
                ComboBox,
                DropDownList,
            };
        }

        public override void Load(BaseGame game)
        {
            base.Load(game);
            AutoSizeAxes = Axes.Y;
            ComboBoxBackgroundBox.Colour = ComboBoxBackgroundColour;
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
            IsListRefreshed = false;
            ValueChanged?.Invoke(this, null);
        }

        public void TriggerListChanged()
        {
            IsListRefreshed = false;
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

        protected virtual void AnimateClose()
        {
            foreach (Drawable child in DropDownList.Children)
            {
                child.MoveToY(0);
                child.Hide();
            }
            DropDownList.Hide();
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
                if (!IsListRefreshed)
                    refreshDropDownList();
                AnimateOpen();
            }
            return true;
        }

        private void itemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Reset)
                SelectedIndex = 0;
            if (e.Action == NotifyCollectionChangedAction.Replace &&
                e.NewStartingIndex <= SelectedIndex && SelectedIndex < e.OldStartingIndex + e.NewItems.Count)
                TriggerValueChanged();
            if (e.Action == NotifyCollectionChangedAction.Add && e.NewStartingIndex <= SelectedIndex)
                SelectedIndex += e.NewItems.Count;
            if (e.Action == NotifyCollectionChangedAction.Remove && e.OldStartingIndex <= SelectedIndex)
            {
                if (SelectedIndex < e.OldStartingIndex + e.OldItems.Count)
                    SelectedIndex = Math.Max(0, e.OldStartingIndex - 1);
                else
                    SelectedIndex -= e.OldItems.Count;
            }
            if (e.Action == NotifyCollectionChangedAction.Move)
            {
                if (e.OldStartingIndex <= SelectedIndex)
                {
                    if (SelectedIndex < e.OldStartingIndex + e.OldItems.Count)
                        SelectedIndex = SelectedIndex - e.OldStartingIndex + e.NewStartingIndex;
                    else if (SelectedIndex < e.NewStartingIndex)
                        SelectedIndex -= e.OldItems.Count;
                }
                else if (e.NewStartingIndex < SelectedIndex)
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
            for (int i = 0; i < Items.Count; i++)
            {
                DropDownMenuItem newItem = (DropDownMenuItem)Activator.CreateInstance(MenuItemType, new[] { this });
                newItem.Item = Items[i];
                newItem.Index = i;
                newItem.Depth = -i;
                newItem.Alpha = 0;

                DropDownList.Add(newItem);
            }

            IsListRefreshed = true;
        }
    }
}
