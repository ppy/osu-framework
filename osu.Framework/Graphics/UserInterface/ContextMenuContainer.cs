// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
using osu.Framework.Caching;
using osu.Framework.Graphics.Containers;
using System;
using System.Collections.Generic;

namespace osu.Framework.Graphics.UserInterface
{
    public class ContextMenuContainer : Container
    {
        private readonly ContextMenu contextMenu;
        protected virtual ContextMenu CreateContextMenu() => new ContextMenu();

        public MenuState State => contextMenu?.State ?? MenuState.Closed;

        public IEnumerable<ContextMenuItem> Items
        {
            set
            {
                if (contextMenu != null)
                {
                    contextMenu.ItemsContainer.Children = value;

                    foreach (var item in Items)
                        item.Action += Close;
                }
            }
            get
            {
                return contextMenu.ItemsContainer.Children;
            }
        }

        public ContextMenuContainer()
        {
            AlwaysReceiveInput = true;
            AutoSizeAxes = Axes.Y;
            Add(contextMenu = CreateContextMenu());
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
            if (contextMenu == null)
                return;
            contextMenu.State = MenuState.Opened;
        }

        public void Close()
        {
            if (contextMenu == null)
                return;
            contextMenu.State = MenuState.Closed;
        }
    }

    public class ContextMenu : Menu<ContextMenuItem>
    {
        protected int FadeDuration;

        protected override void UpdateContentHeight()
        {
            var actualHeight = (RelativeSizeAxes & Axes.Y) > 0 ? 1 : ContentHeight;
            ResizeTo(new Vector2(1, State == MenuState.Opened ? actualHeight : 0), FadeDuration, EasingTypes.OutQuint);
        }
    }
}
