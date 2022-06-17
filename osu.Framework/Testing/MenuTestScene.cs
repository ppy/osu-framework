// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.UserInterface;
using osuTK.Input;

namespace osu.Framework.Testing
{
    /// <summary>
    /// A test scene that provides a set of helper functions and structures for testing a <see cref="Menu"/>.
    /// </summary>
    public abstract class MenuTestScene : ManualInputManagerTestScene
    {
        protected MenuStructure Menus;

        protected void CreateMenu(Func<Menu> creationFunc) => AddStep("create menu", () =>
        {
            Menu menu;
            Child = new Container
            {
                RelativeSizeAxes = Axes.Both,
                Child = menu = creationFunc.Invoke(),
            };

            Menus = new MenuStructure(menu);
        });

        /// <summary>
        /// Click an item in a menu.
        /// </summary>
        /// <param name="menuIndex">The level of menu our click targets.</param>
        /// <param name="itemIndex">The item to click in the menu.</param>
        protected void ClickItem(int menuIndex, int itemIndex)
        {
            InputManager.MoveMouseTo(Menus.GetSubStructure(menuIndex).GetMenuItems()[itemIndex]);
            InputManager.Click(MouseButton.Left);
        }

        /// <summary>
        /// Helper class used to retrieve various internal properties/items from a <see cref="Menu"/>.
        /// </summary>
        protected class MenuStructure
        {
            private readonly Menu menu;

            public MenuStructure(Menu menu)
            {
                this.menu = menu;
            }

            /// <summary>
            /// Retrieves the <see cref="Menu.DrawableMenuItem"/>s of the <see cref="Menu"/> represented by this <see cref="MenuStructure"/>.
            /// </summary>
            public IReadOnlyList<Drawable> GetMenuItems() => menu.ChildrenOfType<FillFlowContainer<Menu.DrawableMenuItem>>().First().Children;

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
