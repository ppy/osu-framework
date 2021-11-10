// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Testing;
using osuTK;
using osuTK.Input;

namespace osu.Framework.Tests.Visual.UserInterface
{
    public class TestSceneNestedMenus : MenuTestScene
    {
        private const int max_depth = 5;
        private const int max_count = 5;

        private Random rng;

        [SetUp]
        public new void SetUp() => rng = new Random(1337);

        protected override Menu CreateMenu() => new ClickOpenMenu(TimePerAction)
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

        private class ClickOpenMenu : BasicMenu
        {
            protected override Menu CreateSubMenu() => new ClickOpenMenu(HoverOpenDelay, false);

            public ClickOpenMenu(double timePerAction, bool topLevel = true)
                : base(Direction.Vertical, topLevel)
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
            AddStep("Click outside", () => InputManager.Click(MouseButton.Left));
            AddAssert("Check AlwaysOpen = true", () => Menus.GetSubMenu(0).State == MenuState.Open);
        }

        /// <summary>
        /// Tests if the hover state on <see cref="Menu.DrawableMenuItem"/>s is valid.
        /// </summary>
        [Test]
        public void TestHoverState()
        {
            AddAssert("Check submenu closed", () => Menus.GetSubMenu(1)?.State != MenuState.Open);
            AddStep("Hover item", () => InputManager.MoveMouseTo(Menus.GetMenuItems()[0]));
            AddAssert("Check item hovered", () => Menus.GetMenuItems()[0].IsHovered);
        }

        /// <summary>
        /// Tests if the <see cref="Menu"/> respects <see cref="Menu.TopLevelMenu"/> = true.
        /// </summary>
        [Test]
        public void TestTopLevelMenu()
        {
            AddStep("Hover item", () => InputManager.MoveMouseTo(Menus.GetSubStructure(0).GetMenuItems()[0]));
            AddAssert("Check closed", () => Menus.GetSubMenu(1)?.State != MenuState.Open);
            AddAssert("Check closed", () => Menus.GetSubMenu(1)?.State != MenuState.Open);
            AddStep("Click item", () => InputManager.Click(MouseButton.Left));
            AddAssert("Check open", () => Menus.GetSubMenu(1).State == MenuState.Open);
        }

        /// <summary>
        /// Tests if clicking once on a menu that has <see cref="Menu.TopLevelMenu"/> opens it, and clicking a second time
        /// closes it.
        /// </summary>
        [Test]
        public void TestDoubleClick()
        {
            AddStep("Click item", () => ClickItem(0, 0));
            AddAssert("Check open", () => Menus.GetSubMenu(1).State == MenuState.Open);
            AddStep("Click item", () => ClickItem(0, 0));
            AddAssert("Check closed", () => Menus.GetSubMenu(1)?.State != MenuState.Open);
        }

        /// <summary>
        /// Tests whether click on <see cref="Menu.DrawableMenuItem"/>s causes sub-menus to instantly appear.
        /// </summary>
        [Test]
        public void TestInstantOpen()
        {
            AddStep("Click item", () => ClickItem(0, 1));
            AddAssert("Check open", () => Menus.GetSubMenu(1).State == MenuState.Open);
            AddStep("Click item", () => ClickItem(1, 0));
            AddAssert("Check open", () => Menus.GetSubMenu(2).State == MenuState.Open);
        }

        /// <summary>
        /// Tests if clicking on an item that has no sub-menu causes the menu to close.
        /// </summary>
        [Test]
        public void TestActionClick()
        {
            AddStep("Click item", () => ClickItem(0, 0));
            AddStep("Click item", () => ClickItem(1, 0));
            AddAssert("Check closed", () => Menus.GetSubMenu(1)?.State != MenuState.Open);
        }

        /// <summary>
        /// Tests if hovering over menu items respects the <see cref="Menu.HoverOpenDelay"/>.
        /// </summary>
        [Test]
        public void TestHoverOpen()
        {
            AddStep("Click item", () => ClickItem(0, 1));
            AddStep("Hover item", () => InputManager.MoveMouseTo(Menus.GetSubStructure(1).GetMenuItems()[0]));
            AddAssert("Check closed", () => Menus.GetSubMenu(2)?.State != MenuState.Open);
            AddAssert("Check open", () => Menus.GetSubMenu(2).State == MenuState.Open);
            AddStep("Hover item", () => InputManager.MoveMouseTo(Menus.GetSubStructure(2).GetMenuItems()[0]));
            AddAssert("Check closed", () => Menus.GetSubMenu(3)?.State != MenuState.Open);
            AddAssert("Check open", () => Menus.GetSubMenu(3).State == MenuState.Open);
        }

        /// <summary>
        /// Tests if hovering over a different item on the main <see cref="Menu"/> will instantly open another menu
        /// and correctly changes the sub-menu items to the new items from the hovered item.
        /// </summary>
        [Test]
        public void TestHoverChange()
        {
            IReadOnlyList<MenuItem> currentItems = null;
            AddStep("Click item", () => { ClickItem(0, 0); });

            AddStep("Get items", () => { currentItems = Menus.GetSubMenu(1).Items; });

            AddAssert("Check open", () => Menus.GetSubMenu(1).State == MenuState.Open);
            AddStep("Hover item", () => InputManager.MoveMouseTo(Menus.GetSubStructure(0).GetMenuItems()[1]));
            AddAssert("Check open", () => Menus.GetSubMenu(1).State == MenuState.Open);

            AddAssert("Check new items", () => !Menus.GetSubMenu(1).Items.SequenceEqual(currentItems));
            AddAssert("Check closed", () =>
            {
                int currentSubMenu = 3;

                while (true)
                {
                    var subMenu = Menus.GetSubMenu(currentSubMenu);
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
            AddStep("Click item", () => ClickItem(0, 2));
            AddStep("Hover item", () => InputManager.MoveMouseTo(Menus.GetSubStructure(1).GetMenuItems()[0]));
            AddAssert("Check closed", () => Menus.GetSubMenu(2)?.State != MenuState.Open);
            AddAssert("Check closed", () => Menus.GetSubMenu(2)?.State != MenuState.Open);

            AddStep("Hover item", () => { InputManager.MoveMouseTo(Menus.GetSubStructure(1).GetMenuItems()[1]); });

            AddAssert("Check closed", () => Menus.GetSubMenu(2)?.State != MenuState.Open);
            AddAssert("Check open", () => Menus.GetSubMenu(2).State == MenuState.Open);

            AddAssert("Check closed", () =>
            {
                int currentSubMenu = 3;

                while (true)
                {
                    var subMenu = Menus.GetSubMenu(currentSubMenu);
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
            AddStep("Click item", () => ClickItem(0, 1));
            AddStep("Click item", () => ClickItem(1, 0));
            AddStep("Click item", () => ClickItem(2, 0));
            AddStep("Click item", () => ClickItem(3, 0));

            for (int i = 3; i >= 1; i--)
            {
                int menuIndex = i;
                AddStep("Hover item", () => InputManager.MoveMouseTo(Menus.GetSubStructure(menuIndex).GetMenuItems()[0]));
                AddAssert("Check submenu open", () => Menus.GetSubMenu(menuIndex + 1).State == MenuState.Open);
                AddStep("Click item", () => InputManager.Click(MouseButton.Left));
                AddAssert("Check all open", () =>
                {
                    for (int j = 0; j <= menuIndex; j++)
                    {
                        int menuIndex2 = j;
                        if (Menus.GetSubMenu(menuIndex2)?.State != MenuState.Open)
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
            AddStep("Click item", () => ClickItem(0, 1));
            AddStep("Click item", () => ClickItem(1, 0));
            AddStep("Click item", () => ClickItem(2, 0));
            AddStep("Click item", () => ClickItem(3, 0));
            AddStep("Click item", () => ClickItem(0, 1));

            AddAssert("Check submenus closed", () =>
            {
                for (int j = 1; j <= 3; j++)
                {
                    int menuIndex2 = j;
                    if (Menus.GetSubMenu(menuIndex2).State == MenuState.Open)
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
            AddStep("Click item", () => ClickItem(0, 1));
            AddStep("Click item", () => ClickItem(1, 0));
            AddStep("Click item", () => ClickItem(2, 0));
            AddStep("Click item", () => ClickItem(3, 0));
            AddStep("Click item", () => ClickItem(4, 0));

            AddAssert("Check submenus closed", () =>
            {
                for (int j = 1; j <= 3; j++)
                {
                    int menuIndex2 = j;
                    if (Menus.GetSubMenu(menuIndex2).State == MenuState.Open)
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
                    AddStep("Click item", () => ClickItem(menuToOpen, itemToOpen));
                }

                if (hoverPrevious && i > 0)
                    AddStep("Hover previous", () => InputManager.MoveMouseTo(Menus.GetSubStructure(i2 - 1).GetMenuItems()[i2 > 1 ? 0 : 1]));

                AddStep("Remove hover", () => InputManager.MoveMouseTo(Vector2.Zero));
                AddStep("Click outside", () => InputManager.Click(MouseButton.Left));
                AddAssert("Check submenus closed", () =>
                {
                    for (int j = 1; j <= i2 + 1; j++)
                    {
                        int menuIndex2 = j;
                        if (Menus.GetSubMenu(menuIndex2).State == MenuState.Open)
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
            AddStep("Click item", () => ClickItem(0, 2));
            AddAssert("Check open", () => Menus.GetSubMenu(1).State == MenuState.Open);

            AddStep("Hover item", () => InputManager.MoveMouseTo(Menus.GetSubStructure(1).GetMenuItems()[1]));
            AddAssert("Check closed 1", () => Menus.GetSubMenu(2)?.State != MenuState.Open);
            AddAssert("Check open", () => Menus.GetSubMenu(2).State == MenuState.Open);
            AddAssert("Check selected index 1", () => Menus.GetSubStructure(1).GetSelectedIndex() == 1);

            AddStep("Change selection", () => Menus.GetSubStructure(1).SetSelectedState(0, MenuItemState.Selected));
            AddAssert("Check selected index", () => Menus.GetSubStructure(1).GetSelectedIndex() == 0);

            AddStep("Change selection", () => Menus.GetSubStructure(1).SetSelectedState(2, MenuItemState.Selected));
            AddAssert("Check selected index 2", () => Menus.GetSubStructure(1).GetSelectedIndex() == 2);

            AddStep("Close menus", () => Menus.GetSubMenu(0).Close());
            AddAssert("Check selected index 4", () => Menus.GetSubStructure(1).GetSelectedIndex() == -1);
        }

        #endregion

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
    }
}
