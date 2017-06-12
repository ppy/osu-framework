// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Caching;
using osu.Framework.Graphics.Containers;
using System;
using System.Collections.Generic;

namespace osu.Framework.Graphics.UserInterface
{
    public class ContextMenu<TItem> : Container
        where TItem : ContextMenuItem
    {
        private readonly Menu<TItem> menu;

        /// <summary>
        /// Creates a new menu. Can be overridden to customize.
        /// </summary>
        protected virtual Menu<TItem> CreateMenu() => new Menu<TItem>();

        /// <summary>
        /// Current state of menu.
        /// </summary>
        public MenuState State => menu.State;

        /// <summary>
        /// Opens the menu.
        /// </summary>
        public void Open() => menu.State = MenuState.Opened;

        /// <summary>
        /// Closes the menu.
        /// </summary>
        public void Close() => menu.State = MenuState.Closed;

        /// <summary>
        /// Items which will be contained in menu.
        /// </summary>
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
            AutoSizeAxes = Axes.Y;
            Add(menu = CreateMenu());
        }

        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();
            if (!menuWidth.IsValid)
            {
                // Sets the width of menu depends on the maximum size of text and content of each item.
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
            if ((invalidation & Invalidation.RequiredParentSizeToFit) > 0)
                menuWidth.Invalidate();
            base.InvalidateFromChild(invalidation);
        }
    }
}
