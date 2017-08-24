// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Caching;
using osu.Framework.Graphics.Containers;
using System;
using System.Collections.Generic;

namespace osu.Framework.Graphics.UserInterface
{
    /// <summary>
    /// Creates a container that manages <see cref="ContextMenuItem"/>s within a <see cref="Menu"/>.
    /// This container will auto-size its width to fit the maximum size of the <see cref="ContextMenuItem"/>s inside <see cref="Items"/>.
    /// </summary>
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
        /// Gets or sets the items to be contained in the menu.
        /// </summary>
        public IEnumerable<TItem> Items
        {
            set
            {
                menu.ItemsContainer.ChildrenEnumerable = value;

                foreach (var item in Items)
                    item.Action += Close;
            }
            get
            {
                return menu.ItemsContainer.Children;
            }
        }

        /// <summary>
        /// Creates a new <see cref="ContextMenu{TItem}"/>.
        /// </summary>
        public ContextMenu()
        {
            AutoSizeAxes = Axes.Y;
            Add(menu = CreateMenu());
        }

        private float computeMenuWidth()
        {
            // The menu items cannot be both relative and auto-sized to fit the entire width of the menu so they (along with the menu)
            // are defined to be relatively-sized on the x-axis. We need to define the size ourselves to give them a valid size.
            float textWidth = 0;
            float contentWidth = 0;

            foreach (var item in Items)
            {
                textWidth = Math.Max(textWidth, item.TextDrawWidth);
                contentWidth = Math.Max(contentWidth, item.ContentDrawWidth);
            }

            return textWidth + contentWidth;
        }

        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();
            if (!menuWidth.IsValid)
            {
                Width = computeMenuWidth();
                menuWidth.Validate();
            }
        }

        private Cached menuWidth = new Cached();

        public override void InvalidateFromChild(Invalidation invalidation)
        {
            if ((invalidation & Invalidation.RequiredParentSizeToFit) > 0)
                menuWidth.Invalidate();
            base.InvalidateFromChild(invalidation);
        }
    }
}
