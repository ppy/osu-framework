// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input;

namespace osu.Framework.Graphics.UserInterface
{
    /// <summary>
    /// A horizontally-tiling bar of <see cref="MenuItem"/> that expand into separate <see cref="Menu"/>s.
    /// </summary>
    public class MenuBar : CompositeDrawable
    {
        /// <summary>
        /// The <see cref="FlowContainer{DrawableMenuBarItem}"/> that handles positioning for the <see cref="DrawableMenuBarItem"/>s.
        /// </summary>
        protected readonly FlowContainer<DrawableMenuBarItem> ItemsContainer;

        /// <summary>
        /// Constructs a new <see cref="MenuBar"/>.
        /// </summary>
        public MenuBar()
        {
            AutoSizeAxes = Axes.Both;

            AddInternal(ItemsContainer = new FillFlowContainer<DrawableMenuBarItem>
            {
                AutoSizeAxes = Axes.Both,
                Direction = FillDirection.Horizontal
            });
        }

        /// <summary>
        /// Gets or sets the <see cref="MenuItem"/>s contained by this <see cref="MenuBar"/>.
        /// </summary>
        public IReadOnlyList<MenuItem> Items
        {
            get { return ItemsContainer.Select(i => i.Item).ToList(); }
            set { value.ForEach(Add); }
        }

        /// <summary>
        /// Adds a <see cref="MenuItem"/> to this <see cref="MenuBar"/>.
        /// </summary>
        /// <param name="item">The <see cref="MenuItem"/> to add.</param>
        public void Add(MenuItem item) => ItemsContainer.Add(CreateDrawableMenuBarItem(item));

        /// <summary>
        /// Removes a <see cref="MenuItem"/> from this <see cref="MenuBar"/>.
        /// </summary>
        /// <param name="item">The <see cref="MenuItem"/> to remove.</param>
        /// <returns>Whether <paramref name="item"/> was successfully removed.</returns>
        public bool Remove(MenuItem item) => ItemsContainer.RemoveAll(i => i.Item == item) > 0;

        /// <summary>
        /// Clears all <see cref="MenuItem"/>s from this <see cref="MenuBar"/>.
        /// </summary>
        public void Clear() => ItemsContainer.Clear();

        protected override bool OnMouseMove(InputState state)
        {
            // Find the previously-opened item
            var previousItem = ItemsContainer.FirstOrDefault(i => i.State == MenuState.Opened);
            if (previousItem == null)
                return false;

            // Find the newly-hovered item
            var newItem = ItemsContainer.FirstOrDefault(i => i.IsHovered);
            if (newItem == null)
                return false;

            // If they're the same item, there's nothing to do
            if (previousItem == newItem)
                return false;

            // Move the opened state to the newly-hovered item
            previousItem.Close();
            newItem.Open();
            return true;
        }

        /// <summary>
        /// Creates the drawable visualisation for a <see cref="MenuItem"/>.
        /// </summary>
        /// <param name="item">The <see cref="MenuItem"/> that is to be visualised.</param>
        /// <returns>The <see cref="DrawableMenuBarItem"/>.</returns>
        protected virtual DrawableMenuBarItem CreateDrawableMenuBarItem(MenuItem item) => new DrawableMenuBarItem(item);

        /// <summary>
        /// A <see cref="CompositeDrawable"/> that visualises a <see cref="MenuItem"/>.
        /// </summary>
        protected class DrawableMenuBarItem : CompositeDrawable
        {
            /// <summary>
            /// The <see cref="Menu"/> that is expanded when this <see cref="DrawableMenuBarItem"/> is clicked.
            /// </summary>
            protected readonly Menu Menu;

            /// <summary>
            /// The <see cref="SpriteText"/> that visualises the <see cref="MenuItem.Text"/>.
            /// </summary>
            protected readonly SpriteText Text;

            /// <summary>
            /// The <see cref="MenuItem"/> that this <see cref="DrawableMenuBarItem"/> visualises.
            /// </summary>
            public readonly MenuItem Item;

            /// <summary>
            /// Constructs a new <see cref="DrawableMenuBarItem"/>.
            /// </summary>
            /// <param name="item">The <see cref="MenuItem"/> which this <see cref="DrawableMenuBarItem"/> will visualise.</param>
            public DrawableMenuBarItem(MenuItem item)
            {
                Item = item;

                AutoSizeAxes = Axes.Both;

                AddRangeInternal(new Drawable[]
                {
                    Text = CreateText(),
                    Menu = CreateMenu()
                });

                Text.Text = item.Text;

                Menu.BypassAutoSizeAxes = Axes.Both;
                Menu.Items = item.Items;

                item.Text.ValueChanged += newText => Text.Text = newText;
            }

            /// <summary>
            /// The current state of the <see cref="Menu"/>.
            /// </summary>
            public MenuState State => Menu.State;

            /// <summary>
            /// Opens the <see cref="Menu"/>.
            /// </summary>
            public void Open()
            {
                Menu.Open();
            }

            /// <summary>
            /// Closes the <see cref="Menu"/>.
            /// </summary>
            public void Close()
            {
                Menu.Close();
            }

            protected override bool OnClick(InputState state)
            {
                switch (Menu.State)
                {
                    case MenuState.Closed:
                        Open();
                        break;
                    case MenuState.Opened:
                        Close();
                        break;
                }

                return true;
            }

            /// <summary>
            /// Creates the <see cref="SpriteText"/> that visualises the <see cref="MenuItem.Text"/>.
            /// </summary>
            /// <returns></returns>
            protected virtual SpriteText CreateText() => new SpriteText
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre
            };

            /// <summary>
            /// Creates the <see cref="Menu"/> that is expanded when this <see cref="DrawableMenuBarItem"/> is clicked.
            /// </summary>
            /// <returns></returns>
            protected virtual Menu CreateMenu() => new Menu { Anchor = Anchor.BottomLeft };
        }
    }
}
