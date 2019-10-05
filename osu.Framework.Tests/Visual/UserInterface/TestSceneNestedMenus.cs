// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Testing;

namespace osu.Framework.Tests.Visual.UserInterface
{
    public class TestSceneNestedMenus : ManualInputManagerTestScene
    {
        private const int max_depth = 5;
        private const int max_count = 5;

        private Random rng;

        public MenuStructure Menus { get; set; }

        public void SetUp(double timePerAction)
        {
            Clear();

            rng = new Random(1337);

            Menu menu;
            Children = new Drawable[]
            {
                new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Child = menu = CreateMenu(timePerAction)
                }
            };

            Menus = new MenuStructure(menu);
        }

        public Menu CreateMenu(double timePerAction) => new ClickOpenMenu(timePerAction)
        {
            Anchor = Anchor.Centre,
            Origin = Anchor.Centre,
            Items = new[]
            {
                GenerateRandomMenuItem("First"),
                GenerateRandomMenuItem("Second"),
                GenerateRandomMenuItem("Third"),
            }
        };

        public MenuItem GenerateRandomMenuItem(string name = "Menu Item", int currDepth = 1)
        {
            var item = new MenuItem(name);

            if (currDepth == max_depth)
                return item;

            int subCount = rng.Next(0, max_count);
            var subItems = new List<MenuItem>();
            for (int i = 0; i < subCount; i++)
                subItems.Add(GenerateRandomMenuItem(item.Text + $" #{i + 1}", currDepth + 1));

            item.Items = subItems;
            return item;
        }

        public TestSceneNestedMenus()
        {
        }

        private class ClickOpenMenu : BasicMenu
        {
            protected override Menu CreateSubMenu() => new ClickOpenMenu(HoverOpenDelay, false);

            public ClickOpenMenu(double timePerAction, bool topLevel = true)
                : base(Direction.Vertical, topLevel)
            {
                HoverOpenDelay = timePerAction;
            }
        }

        /// <summary>
        /// Helper class used to retrieve various internal properties/items from a <see cref="Menu"/>.
        /// </summary>
        public class MenuStructure
        {
            private readonly Menu menu;

            public MenuStructure(Menu menu)
            {
                this.menu = menu;
            }

            /// <summary>
            /// Retrieves the <see cref="Menu.DrawableMenuItem"/>s of the <see cref="Menu"/> represented by this <see cref="MenuStructure"/>.
            /// </summary>
            public IReadOnlyList<Drawable> GetMenuItems()
            {
                var contents = (CompositeDrawable)menu.InternalChildren[0];
                var contentContainer = (CompositeDrawable)contents.InternalChildren[1];
                return ((CompositeDrawable)((CompositeDrawable)contentContainer.InternalChildren[0]).InternalChildren[0]).InternalChildren;
            }

            /// <summary>
            /// Finds the <see cref="Menu.DrawableMenuItem"/> index in the <see cref="Menu"/> represented by this <see cref="MenuStructure"/> that
            /// has <see cref="Menu.DrawableMenuItem.State"/> set to <see cref="MenuItemState.Selected"/>.
            /// </summary>
            public int GetSelectedIndex()
            {
                var items = GetMenuItems();

                for (int i = 0; i < items.Count; i++)
                {
                    var state = (MenuItemState)(items[i]?.GetType().GetProperty("State")?.GetValue(items[i]) ?? MenuItemState.NotSelected);
                    if (state == MenuItemState.Selected)
                        return i;
                }

                return -1;
            }

            /// <summary>
            /// Sets the <see cref="Menu.DrawableMenuItem"/> <see cref="Menu.DrawableMenuItem.State"/> at the specified index to a specified state.
            /// </summary>
            /// <param name="index">The index of the <see cref="Menu.DrawableMenuItem"/> to set the state of.</param>
            /// <param name="state">The state to be set.</param>
            public void SetSelectedState(int index, MenuItemState state)
            {
                var item = GetMenuItems()[index];
                item.GetType().GetProperty("State")?.SetValue(item, state);
            }

            /// <summary>
            /// Retrieves the sub-<see cref="Menu"/> at an index-offset from the current <see cref="Menu"/>.
            /// </summary>
            /// <param name="index">The sub-<see cref="Menu"/> index. An index of 0 is the <see cref="Menu"/> represented by this <see cref="MenuStructure"/>.</param>
            public Menu GetSubMenu(int index)
            {
                var currentMenu = menu;

                for (int i = 0; i < index; i++)
                {
                    if (currentMenu == null)
                        break;

                    var container = (CompositeDrawable)currentMenu.InternalChildren[1];

                    // ReSharper disable once ArrangeRedundantParentheses
                    // Broken resharper inspection (https://youtrack.jetbrains.com/issue/RIDER-19843)
                    currentMenu = (container.InternalChildren.Count > 0 ? container.InternalChildren[0] : null) as Menu;
                }

                return currentMenu;
            }

            /// <summary>
            /// Generates a new <see cref="MenuStructure"/> for the a sub-<see cref="Menu"/>.
            /// </summary>
            /// <param name="index">The sub-<see cref="Menu"/> index to generate the <see cref="MenuStructure"/> for. An index of 0 is the <see cref="Menu"/> represented by this <see cref="MenuStructure"/>.</param>
            public MenuStructure GetSubStructure(int index) => new MenuStructure(GetSubMenu(index));
        }
    }
}
