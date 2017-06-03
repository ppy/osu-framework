// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
using OpenTK.Graphics;
using osu.Framework.Caching;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics.Containers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace osu.Framework.Graphics.UserInterface
{
    public class ContextMenuContainer<TItem> : Container
        where TItem : ContextMenuItem
    {
        private readonly ContextMenu contextMenu;

        public MenuState State => contextMenu?.State ?? MenuState.Closed;

        public IEnumerable<TItem> Items
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
            Add(contextMenu = new ContextMenu());
        }

        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();
            if (!menuWidth.IsValid)
            {
                menuWidth.Refresh(() =>
                {
                    float width = 0;

                    foreach (var item in Items)
                        width = Math.Max(width, item.DrawWidth);
                    return width;
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

        public class ContextMenu : Menu<TItem>
        {
            private const int fade_duration = 250;

            public ContextMenu()
            {
                CornerRadius = 5;
                ItemsContainer.Padding = new MarginPadding { Vertical = ContextMenuItem.MARGIN_VERTICAL };
                Masking = true;
                EdgeEffect = new EdgeEffect
                {
                    Type = EdgeEffectType.Shadow,
                    Colour = Color4.Black.Opacity(0.25f),
                    Radius = 4,
                };
            }

            protected override void AnimateOpen() => FadeIn(fade_duration, EasingTypes.OutQuint);

            protected override void AnimateClose() => FadeOut(fade_duration, EasingTypes.OutQuint);

            protected override void UpdateContentHeight()
            {
                var actualHeight = (RelativeSizeAxes & Axes.Y) > 0 ? 1 : ContentHeight;
                ResizeTo(new Vector2(1, State == MenuState.Opened ? actualHeight : 0), fade_duration, EasingTypes.OutQuint);
            }
        }
    }
}
