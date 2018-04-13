// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Testing;
using osu.Framework.Testing.Input;
using OpenTK;
using OpenTK.Input;

namespace osu.Framework.Tests.Visual
{
    public class TestCaseNestedMenus : TestCase
    {
        private const int max_depth = 5;
        private const int max_count = 5;

        public override IReadOnlyList<Type> RequiredTypes => new[] { typeof(Menu) };

        private Random rng;

        private ManualInputManager inputManager;
        private MenuStructure menus;

        [SetUp]
        public void SetUp()
        {
            Clear();

            rng = new Random(1337);

            Menu menu;
            Add(inputManager = new ManualInputManager
            {
                Children = new Drawable[]
                {
                    new CursorContainer(),
                    new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                        Child = menu = createMenu()
                    }
                }
            });

            menus = new MenuStructure(menu);
        }

        private Menu createMenu() => new ClickOpenMenu(TimePerAction)
        {
            Anchor = Anchor.Centre,
            Origin = Anchor.Centre,
            Items = new[]
            {
                generateRandomMenuItem("First"),
                generateRandomMenuItem("Second"),
                generateRandomMenuItem("Third"),
            }
        };

        private class ClickOpenMenu : Menu
        {
            protected override Menu CreateSubMenu() => new ClickOpenMenu(HoverOpenDelay, false);

            public ClickOpenMenu(double timePerAction, bool topLevel = true) : base(Direction.Vertical, topLevel)
            {
                HoverOpenDelay = timePerAction;
            }
        }

        #region Test Cases

        /// <summary>
        /// Tests if the <see cref="Menu"/> respects <see cref="Menu.TopLevelMenu"/> = true, by not alowing it to be closed
        /// when a click happens outside the <see cref="Menu"/>.
        /// </summary>
        [Test]
        public void TestAlwaysOpen()
        {
            AddStep("Click outside", () => inputManager.Click(MouseButton.Left));
            AddAssert("Check AlwaysOpen = true", () => menus.GetSubMenu(0).State == MenuState.Open);
        }

        /// <summary>
        /// Tests if the hover state on <see cref="Menu.DrawableMenuItem"/>s is valid.
        /// </summary>
        [Test]
        public void TestHoverState()
        {
            AddAssert("Check submenu closed", () => menus.GetSubMenu(1)?.State != MenuState.Open);
            AddStep("Hover item", () => inputManager.MoveMouseTo(menus.GetMenuItems()[0]));
            AddAssert("Check item hovered", () => menus.GetMenuItems()[0].IsHovered);
        }

        /// <summary>
        /// Tests if the <see cref="Menu"/> respects <see cref="Menu.TopLevelMenu"/> = true.
        /// </summary>
        [Test]
        public void TestTopLevelMenu()
        {
            AddStep("Hover item", () => inputManager.MoveMouseTo(menus.GetSubStructure(0).GetMenuItems()[0]));
            AddAssert("Check closed", () => menus.GetSubMenu(1)?.State != MenuState.Open);
            AddAssert("Check closed", () => menus.GetSubMenu(1)?.State != MenuState.Open);
            AddStep("Click item", () => inputManager.Click(MouseButton.Left));
            AddAssert("Check open", () => menus.GetSubMenu(1).State == MenuState.Open);
        }

        /// <summary>
        /// Tests if clicking once on a menu that has <see cref="Menu.TopLevelMenu"/> opens it, and clicking a second time
        /// closes it.
        /// </summary>
        [Test]
        public void TestDoubleClick()
        {
            AddStep("Click item", () => clickItem(0, 0));
            AddAssert("Check open", () => menus.GetSubMenu(1).State == MenuState.Open);
            AddStep("Click item", () => clickItem(0, 0));
            AddAssert("Check closed", () => menus.GetSubMenu(1)?.State != MenuState.Open);
        }

        /// <summary>
        /// Tests whether click on <see cref="Menu.DrawableMenuItem"/>s causes sub-menus to instantly appear.
        /// </summary>
        [Test]
        public void TestInstantOpen()
        {
            AddStep("Click item", () => clickItem(0, 1));
            AddAssert("Check open", () => menus.GetSubMenu(1).State == MenuState.Open);
            AddStep("Click item", () => clickItem(1, 0));
            AddAssert("Check open", () => menus.GetSubMenu(2).State == MenuState.Open);
        }

        /// <summary>
        /// Tests if clicking on an item that has no sub-menu causes the menu to close.
        /// </summary>
        [Test]
        public void TestActionClick()
        {
            AddStep("Click item", () => clickItem(0, 0));
            AddStep("Click item", () => clickItem(1, 0));
            AddAssert("Check closed", () => menus.GetSubMenu(1)?.State != MenuState.Open);
        }

        /// <summary>
        /// Tests if hovering over menu items respects the <see cref="Menu.HoverOpenDelay"/>.
        /// </summary>
        [Test]
        public void TestHoverOpen()
        {
            AddStep("Click item", () => clickItem(0, 1));
            AddStep("Hover item", () => inputManager.MoveMouseTo(menus.GetSubStructure(1).GetMenuItems()[0]));
            AddAssert("Check closed", () => menus.GetSubMenu(2)?.State != MenuState.Open);
            AddAssert("Check open", () => menus.GetSubMenu(2).State == MenuState.Open);
            AddStep("Hover item", () => inputManager.MoveMouseTo(menus.GetSubStructure(2).GetMenuItems()[0]));
            AddAssert("Check closed", () => menus.GetSubMenu(3)?.State != MenuState.Open);
            AddAssert("Check open", () => menus.GetSubMenu(3).State == MenuState.Open);
        }

        /// <summary>
        /// Tests if hovering over a different item on the main <see cref="Menu"/> will instantly open another menu
        /// and correctly changes the sub-menu items to the new items from the hovered item.
        /// </summary>
        [Test]
        public void TestHoverChange()
        {
            IReadOnlyList<MenuItem> currentItems = null;
            AddStep("Click item", () =>
            {
                clickItem(0, 0);
            });

            AddStep("Get items", () =>
            {
                currentItems = menus.GetSubMenu(1).Items;
            });

            AddAssert("Check open", () => menus.GetSubMenu(1).State == MenuState.Open);
            AddStep("Hover item", () => inputManager.MoveMouseTo(menus.GetSubStructure(0).GetMenuItems()[1]));
            AddAssert("Check open", () => menus.GetSubMenu(1).State == MenuState.Open);

            AddAssert("Check new items", () => !menus.GetSubMenu(1).Items.SequenceEqual(currentItems));
            AddAssert("Check closed", () =>
            {
                int currentSubMenu = 3;
                while (true)
                {
                    var subMenu = menus.GetSubMenu(currentSubMenu);
                    if (subMenu == null)
                        break;

                    if (subMenu.State == MenuState.Open)
                        return false;
                    currentSubMenu++;
                }

                return true;
            });
        }

        /// <summary>
        /// Tests whether hovering over a different item on a sub-menu opens a new sub-menu in a delayed fashion
        /// and correctly changes the sub-menu items to the new items from the hovered item.
        /// </summary>
        [Test]
        public void TestDelayedHoverChange()
        {
            AddStep("Click item", () => clickItem(0, 2));
            AddStep("Hover item", () => inputManager.MoveMouseTo(menus.GetSubStructure(1).GetMenuItems()[0]));
            AddAssert("Check closed", () => menus.GetSubMenu(2)?.State != MenuState.Open);
            AddAssert("Check closed", () => menus.GetSubMenu(2)?.State != MenuState.Open);

            AddStep("Hover item", () =>
            {
                inputManager.MoveMouseTo(menus.GetSubStructure(1).GetMenuItems()[1]);
            });

            AddAssert("Check closed", () => menus.GetSubMenu(2)?.State != MenuState.Open);
            AddAssert("Check open", () => menus.GetSubMenu(2).State == MenuState.Open);

            AddAssert("Check closed", () =>
            {
                int currentSubMenu = 3;
                while (true)
                {
                    var subMenu = menus.GetSubMenu(currentSubMenu);
                    if (subMenu == null)
                        break;

                    if (subMenu.State == MenuState.Open)
                        return false;
                    currentSubMenu++;
                }

                return true;
            });
        }

        /// <summary>
        /// Tests whether clicking on <see cref="Menu"/>s that have opened sub-menus don't close the sub-menus.
        /// Then tests hovering in reverse order to make sure only the lower level menus close.
        /// </summary>
        [Test]
        public void TestMenuClicksDontClose()
        {
            AddStep("Click item", () => clickItem(0, 1));
            AddStep("Click item", () => clickItem(1, 0));
            AddStep("Click item", () => clickItem(2, 0));
            AddStep("Click item", () => clickItem(3, 0));

            for (int i = 3; i >= 1; i--)
            {
                int menuIndex = i;
                AddStep("Hover item", () => inputManager.MoveMouseTo(menus.GetSubStructure(menuIndex).GetMenuItems()[0]));
                AddAssert("Check submenu open", () => menus.GetSubMenu(menuIndex + 1).State == MenuState.Open);
                AddStep("Click item", () => inputManager.Click(MouseButton.Left));
                AddAssert("Check all open", () =>
                {
                    for (int j = 0; j <= menuIndex; j++)
                    {
                        int menuIndex2 = j;
                        if (menus.GetSubMenu(menuIndex2)?.State != MenuState.Open)
                            return false;
                    }

                    return true;
                });
            }
        }

        /// <summary>
        /// Tests whether clicking on the <see cref="Menu"/> that has <see cref="Menu.TopLevelMenu"/> closes all sub menus.
        /// </summary>
        [Test]
        public void TestMenuClickClosesSubMenus()
        {
            AddStep("Click item", () => clickItem(0, 1));
            AddStep("Click item", () => clickItem(1, 0));
            AddStep("Click item", () => clickItem(2, 0));
            AddStep("Click item", () => clickItem(3, 0));
            AddStep("Click item", () => clickItem(0, 1));

            AddAssert("Check submenus closed", () =>
            {
                for (int j = 1; j <= 3; j++)
                {
                    int menuIndex2 = j;
                    if (menus.GetSubMenu(menuIndex2).State == MenuState.Open)
                        return false;
                }

                return true;
            });
        }

        /// <summary>
        /// Tests whether clicking on an action in a sub-menu closes all <see cref="Menu"/>s.
        /// </summary>
        [Test]
        public void TestActionClickClosesMenus()
        {
            AddStep("Click item", () => clickItem(0, 1));
            AddStep("Click item", () => clickItem(1, 0));
            AddStep("Click item", () => clickItem(2, 0));
            AddStep("Click item", () => clickItem(3, 0));
            AddStep("Click item", () => clickItem(4, 0));

            AddAssert("Check submenus closed", () =>
            {
                for (int j = 1; j <= 3; j++)
                {
                    int menuIndex2 = j;
                    if (menus.GetSubMenu(menuIndex2).State == MenuState.Open)
                        return false;
                }

                return true;
            });
        }

        /// <summary>
        /// Tests whether clicking outside the <see cref="Menu"/> structure closes all sub-menus.
        /// </summary>
        /// <param name="hoverPrevious">Whether the previous menu should first be hovered before clicking outside.</param>
        [TestCase(false)]
        [TestCase(true)]
        public void TestClickingOutsideClosesMenus(bool hoverPrevious)
        {
            for (int i = 0; i <= 3; i++)
            {
                int i2 = i;

                for (int j = 0; j <= i; j++)
                {
                    int menuToOpen = j;
                    int itemToOpen = menuToOpen == 0 ? 1 : 0;
                    AddStep("Click item", () => clickItem(menuToOpen, itemToOpen));
                }

                if (hoverPrevious && i > 0)
                    AddStep("Hover previous", () => inputManager.MoveMouseTo(menus.GetSubStructure(i2 - 1).GetMenuItems()[i2 > 1 ? 0 : 1]));

                AddStep("Remove hover", () => inputManager.MoveMouseTo(Vector2.Zero));
                AddStep("Click outside", () => inputManager.Click(MouseButton.Left));
                AddAssert("Check submenus closed", () =>
                {
                    for (int j = 1; j <= i2 + 1; j++)
                    {
                        int menuIndex2 = j;
                        if (menus.GetSubMenu(menuIndex2).State == MenuState.Open)
                            return false;
                    }

                    return true;
                });
            }
        }

        /// <summary>
        /// Opens some menus and then changes the selected item.
        /// </summary>
        [Test]
        public void TestSelectedState()
        {
            AddStep("Click item", () => clickItem(0, 2));
            AddAssert("Check open", () => menus.GetSubMenu(1).State == MenuState.Open);

            AddStep("Hover item", () => inputManager.MoveMouseTo(menus.GetSubStructure(1).GetMenuItems()[1]));
            AddAssert("Check closed 1", () => menus.GetSubMenu(2)?.State != MenuState.Open);
            AddAssert("Check open", () => menus.GetSubMenu(2).State == MenuState.Open);
            AddAssert("Check selected index 1", () => menus.GetSubStructure(1).GetSelectedIndex() == 1);

            AddStep("Change selection", () => menus.GetSubStructure(1).SetSelectedState(0, MenuItemState.Selected));
            AddAssert("Check selected index", () => menus.GetSubStructure(1).GetSelectedIndex() == 0);

            AddStep("Change selection", () => menus.GetSubStructure(1).SetSelectedState(2, MenuItemState.Selected));
            AddAssert("Check selected index 2", () => menus.GetSubStructure(1).GetSelectedIndex() == 2);

            AddStep("Close menus", () => menus.GetSubMenu(0).Close());
            AddAssert("Check selected index 4", () => menus.GetSubStructure(1).GetSelectedIndex() == -1);
        }
        #endregion

        /// <summary>
        /// Click an item in a menu.
        /// </summary>
        /// <param name="menuIndex">The level of menu our click targets.</param>
        /// <param name="itemIndex">The item to click in the menu.</param>
        private void clickItem(int menuIndex, int itemIndex)
        {
            inputManager.MoveMouseTo(menus.GetSubStructure(menuIndex).GetMenuItems()[itemIndex]);
            inputManager.Click(MouseButton.Left);
        }

        private MenuItem generateRandomMenuItem(string name = "Menu Item", int currDepth = 1)
        {
            var item = new MenuItem(name);

            if (currDepth == max_depth)
                return item;

            int subCount = rng.Next(0, max_count);
            var subItems = new List<MenuItem>();
            for (int i = 0; i < subCount; i++)
                subItems.Add(generateRandomMenuItem(item.Text + $" #{i + 1}", currDepth + 1));

            item.Items = subItems;
            return item;
        }

        /// <summary>
        /// Helper class used to retrieve various internal properties/items from a <see cref="Menu"/>.
        /// </summary>
        private class MenuStructure
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
