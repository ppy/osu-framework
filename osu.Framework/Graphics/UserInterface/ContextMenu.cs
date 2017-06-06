// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
using osu.Framework.Caching;
using osu.Framework.Graphics.Containers;
using System;
using System.Collections.Generic;

namespace osu.Framework.Graphics.UserInterface
{
    public class ContextMenu : Container
    {
        private readonly LocalMenu menu;

        protected int FadeDuration { set { menu.FadeDuration = value; } }

        public MenuState State => menu?.State ?? MenuState.Closed;

        public IEnumerable<ContextMenuItem> Items
        {
            set
            {
                if (menu != null)
                {
                    menu.ItemsContainer.Children = value;

                    foreach (var item in Items)
                        item.Action += Close;
                }
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
            Add(menu = new LocalMenu());
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

        public void Open()
        {
            if (menu == null)
                return;
            menu.State = MenuState.Opened;
        }

        public void Close()
        {
            if (menu == null)
                return;
            menu.State = MenuState.Closed;
        }

        private class LocalMenu : Menu<ContextMenuItem>
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
