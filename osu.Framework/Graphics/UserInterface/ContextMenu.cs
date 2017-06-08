// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
using osu.Framework.Caching;
using osu.Framework.Graphics.Containers;
using System;
using System.Collections.Generic;

namespace osu.Framework.Graphics.UserInterface
{
    public class ContextMenu<TItem> : Container
        where TItem : ContextMenuItem
    {
        private readonly CustomMenu menu;

        protected virtual CustomMenu CreateCustomMenu() => new CustomMenu();

        protected int FadeDuration { set { menu.FadeDuration = value; } }

        public MenuState State => menu.State;

        public void Open() => menu.State = MenuState.Opened;
        public void Close() => menu.State = MenuState.Closed;

        public IEnumerable<TItem> Items
        {
            set
            {
                menu.ItemsContainer.Children = value;

                foreach (var item in Items)
                    item.Action += Close;
            }
            get
            {
                return menu.ItemsContainer.Children;
            }
        }

        public ContextMenu()
        {
            AlwaysReceiveInput = true;
            AutoSizeAxes = Axes.Y;
            Add(menu = CreateCustomMenu());
        }

        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();
            if (!menuWidth.IsValid)
            {
                menuWidth.Refresh(() =>
                {
                    float textWidth = 0;
                    float contentWidth = 0;

                    foreach (var item in Items)
                    {
                        textWidth = Math.Max(textWidth, item.TextDrawWidth);
                        contentWidth = Math.Max(contentWidth, item.ContentDrawWidth);
                    }

                    return textWidth + contentWidth;
                });

                Width = menuWidth.Value;
            }
        }

        private Cached<float> menuWidth = new Cached<float>();

        public override void InvalidateFromChild(Invalidation invalidation)
        {
            menuWidth.Invalidate();
            base.InvalidateFromChild(invalidation);
        }

        public class CustomMenu : Menu<TItem>
        {
            public int FadeDuration;

            protected override void UpdateContentHeight()
            {
                var actualHeight = (RelativeSizeAxes & Axes.Y) > 0 ? 1 : ContentHeight;
                ResizeTo(new Vector2(1, State == MenuState.Opened ? actualHeight : 0), FadeDuration, EasingTypes.OutQuint);
            }
        }
    }
}
