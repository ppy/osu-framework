// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input;
using osu.Framework.Input.Handlers;
using osu.Framework.Platform;
using osu.Framework.Testing;
using OpenTK;
using OpenTK.Input;
using MouseState = osu.Framework.Input.MouseState;

namespace osu.Framework.Desktop.Tests.Visual
{
    internal class TestCaseNestedMenus : TestCase
    {
        private const int max_depth = 5;
        private const int max_count = 5;

        private Random rng;

        protected override double TimePerAction => 100;

        private readonly ManualInputManager inputManager;
        private readonly Container menuContainer;
        private MenuStructure menus;

        public TestCaseNestedMenus()
        {
            inputManager = new ManualInputManager
            {
                Children = new Drawable[]
                {
                    menuContainer = new Container { RelativeSizeAxes = Axes.Both },
                    new CursorContainer()
                }
            };

            Add(inputManager);

            testReset(false);

            testAlwaysOpen();
            testHoverState();
            testRequireClickToOpen();
            testDoubleClick();
            testActionClick();
            testInstantOpen();
            testHoverOpen();
            testHoverChange();
            testDelayedHoverChange();
            testMenuClicksDontClose();
            testMenuClickClosesSubMenus();
            testActionClickClosesMenus();
            testClickingOutsideClosesMenus(false);
            testClickingOutsideClosesMenus(true);

            AddStep("Give back control", () => testReset(false, true));
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            RunAllSteps();
        }

        #region Test Cases
        /// <summary>
        /// Blocks all user input and resets the <see cref="Menu"/>.
        /// </summary>
        private void testReset(bool step = true, bool userControl = false)
        {
            var reset = new Action(() =>
            {
                rng = new Random(1337);

                var menu = new Menu(Direction.Vertical)
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    AlwaysOpen = true,
                    RequireClickToOpen = true,
                    HoverOpenDelay = TimePerAction,
                    Items = new[]
                    {
                        generateRandomMenuItem("First"),
                        generateRandomMenuItem("Second"),
                        generateRandomMenuItem("Third"),
                    }
                };

                menuContainer.Clear();
                menuContainer.Add(menu);
                menus = new MenuStructure(menu);

                inputManager.UseParentState = userControl;
                inputManager.MoveMouseTo(Vector2.Zero);
            });

            if (step)
                AddStep("Reset", reset);
            else
                reset();
        }

        /// <summary>
        /// Tests if the <see cref="Menu"/> respects <see cref="Menu.AlwaysOpen"/> = true, by not alowing it to be closed
        /// when a click happens outside the <see cref="Menu"/>.
        /// </summary>
        private void testAlwaysOpen()
        {
            testReset();

            AddStep("Click outside", () => inputManager.Click(MouseButton.Left));
            AddAssert("Check AlwaysOpen = true", () => menus.GetSubMenu(0).State == MenuState.Opened);
        }

        /// <summary>
        /// Tests if the hover state on <see cref="Menu.DrawableMenuItem"/>s is valid.
        /// </summary>
        private void testHoverState()
        {
            testReset();

            AddAssert("Check submenu closed", () => menus.GetSubMenu(1).State == MenuState.Closed);
            AddStep("Hover item", () => inputManager.MoveMouseTo(menus.GetMenuItem(0)));
            AddAssert("Check item hovered", () => menus.GetMenuItem(0).IsHovered);
        }

        /// <summary>
        /// Tests if the <see cref="Menu"/> respects <see cref="Menu.RequireClickToOpen"/> = true.
        /// </summary>
        private void testRequireClickToOpen()
        {
            testReset();

            AddStep("Hover item", () => inputManager.MoveMouseTo(menus.GetSubStructure(0).GetMenuItem(0)));
            AddAssert("Check closed", () => menus.GetSubMenu(1).State == MenuState.Closed);
            AddAssert("Check closed", () => menus.GetSubMenu(1).State == MenuState.Closed);
            AddStep("Click item", () => inputManager.Click(MouseButton.Left));
            AddAssert("Check open", () => menus.GetSubMenu(1).State == MenuState.Opened);
        }

        /// <summary>
        /// Tests if clicking once on a menu that has <see cref="Menu.RequireClickToOpen"/> opens it, and clicking a second time
        /// closes it.
        /// </summary>
        private void testDoubleClick()
        {
            testReset();

            AddStep("Click item", () => clickItem(0, 0));
            AddAssert("Check open", () => menus.GetSubMenu(1).State == MenuState.Opened);
            AddStep("Click item", () => clickItem(0, 0));
            AddAssert("Check closed", () => menus.GetSubMenu(1).State == MenuState.Closed);
        }

        /// <summary>
        /// Tests whether click on <see cref="Menu.DrawableMenuItem"/>s causes sub-menus to instantly appear.
        /// </summary>
        private void testInstantOpen()
        {
            testReset();

            AddStep("Click item", () => clickItem(0, 1));
            AddAssert("Check open", () => menus.GetSubMenu(1).State == MenuState.Opened);
            AddStep("Click item", () => clickItem(1, 0));
            AddAssert("Check open", () => menus.GetSubMenu(2).State == MenuState.Opened);
        }

        /// <summary>
        /// Tests if clicking on an item that has no sub-menu causes the menu to close.
        /// </summary>
        private void testActionClick()
        {
            testReset();

            AddStep("Click item", () => clickItem(0, 0));
            AddStep("Click item", () => clickItem(1, 0));
            AddAssert("Check closed", () => menus.GetSubMenu(1).State == MenuState.Closed);
        }

        /// <summary>
        /// Tests if hovering over menu items respects the <see cref="Menu.HoverOpenDelay"/>.
        /// </summary>
        private void testHoverOpen()
        {
            testReset();

            AddStep("Click item", () => clickItem(0, 1));
            AddStep("Hover item", () => inputManager.MoveMouseTo(menus.GetSubStructure(1).GetMenuItem(0)));
            AddAssert("Check closed", () => menus.GetSubMenu(2).State == MenuState.Closed);
            AddAssert("Check open", () => menus.GetSubMenu(2).State == MenuState.Opened);
            AddStep("Hover item", () => inputManager.MoveMouseTo(menus.GetSubStructure(2).GetMenuItem(0)));
            AddAssert("Check closed", () => menus.GetSubMenu(3).State == MenuState.Closed);
            AddAssert("Check open", () => menus.GetSubMenu(3).State == MenuState.Opened);
        }

        /// <summary>
        /// Tests if hovering over a different item on the main <see cref="Menu"/> will instantly open another menu
        /// and correctly changes the sub-menu items to the new items from the hovered item.
        /// </summary>
        private void testHoverChange()
        {
            testReset();

            IReadOnlyList<MenuItem> currentItems = null;
            AddStep("Click item", () =>
            {
                clickItem(0, 0);
                currentItems = menus.GetSubMenu(1).Items;
            });

            AddAssert("Check open", () => menus.GetSubMenu(1).State == MenuState.Opened);
            AddStep("Hover item", () => inputManager.MoveMouseTo(menus.GetSubStructure(0).GetMenuItem(1)));
            AddAssert("Check open", () => menus.GetSubMenu(1).State == MenuState.Opened);

            AddAssert("Check new items", () => !menus.GetSubMenu(1).Items.SequenceEqual(currentItems));
        }

        /// <summary>
        /// Tests whether hovering over a different item on a sub-menu opens a new sub-menu in a delayed fashion
        /// and correctly changes the sub-menu items to the new items from the hovered item.
        /// </summary>
        private void testDelayedHoverChange()
        {
            testReset();

            AddStep("Click item", () => clickItem(0, 2));
            AddStep("Hover item", () => inputManager.MoveMouseTo(menus.GetSubStructure(1).GetMenuItem(0)));
            AddAssert("Check closed", () => menus.GetSubMenu(2).State == MenuState.Closed);
            AddAssert("Check open", () => menus.GetSubMenu(2).State == MenuState.Opened);

            IReadOnlyList<MenuItem> currentItems = null;
            AddStep("Hover item", () =>
            {
                currentItems = menus.GetSubMenu(2).Items;
                inputManager.MoveMouseTo(menus.GetSubStructure(1).GetMenuItem(1));
            });

            AddAssert("Check open", () => menus.GetSubMenu(1).State == MenuState.Opened);
            AddAssert("Check open", () => menus.GetSubMenu(1).State == MenuState.Opened);

            AddAssert("Check new items", () => !menus.GetSubMenu(2).Items.SequenceEqual(currentItems));
        }

        /// <summary>
        /// Tests whether clicking on <see cref="Menu"/>s that have opened sub-menus don't close the sub-menus.
        /// </summary>
        private void testMenuClicksDontClose()
        {
            testReset();

            AddStep("Click item", () => clickItem(0, 1));
            AddStep("Click item", () => clickItem(1, 0));
            AddStep("Click item", () => clickItem(2, 0));
            AddStep("Click item", () => clickItem(3, 0));

            for (int i = 3; i >= 1; i--)
            {
                int menuIndex = i;
                AddStep("Hover item", () => inputManager.MoveMouseTo(menus.GetSubStructure(menuIndex).GetMenuItem(0)));
                AddAssert("Check submenu open", () => menus.GetSubMenu(menuIndex + 1).State == MenuState.Opened);
                AddStep("Click item", () => inputManager.Click(MouseButton.Left));
                AddAssert("Check all open", () =>
                {
                    for (int j = 0; j <= 3; j++)
                    {
                        int menuIndex2 = j;
                        if (menus.GetSubMenu(menuIndex2).State == MenuState.Closed)
                            return false;
                    }

                    return true;
                });
            }
        }

        /// <summary>
        /// Tests whether clicking on the <see cref="Menu"/> that has <see cref="Menu.RequireClickToOpen"/> closes all sub menus.
        /// </summary>
        private void testMenuClickClosesSubMenus()
        {
            testReset();

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
                    if (menus.GetSubMenu(menuIndex2).State == MenuState.Opened)
                        return false;
                }

                return true;
            });
        }

        /// <summary>
        /// Tests whether clicking on an action in a sub-menu closes all <see cref="Menu"/>s.
        /// </summary>
        private void testActionClickClosesMenus()
        {
            testReset();

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
                    if (menus.GetSubMenu(menuIndex2).State == MenuState.Opened)
                        return false;
                }

                return true;
            });
        }

        /// <summary>
        /// Tests whether clicking outside the <see cref="Menu"/> structure closes all sub-menus.
        /// </summary>
        /// <param name="hoverPrevious">Whether the previous menu should first be hovered before clicking outside.</param>
        private void testClickingOutsideClosesMenus(bool hoverPrevious)
        {
            testReset();

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
                    AddStep("Hover previous", () => inputManager.MoveMouseTo(menus.GetSubStructure(i2 - 1).GetMenuItem(i2 > 1 ? 0 : 1)));

                AddStep("Remove hover", () => inputManager.MoveMouseTo(Vector2.Zero));
                AddStep("Click outside", () => inputManager.Click(MouseButton.Left));
                AddAssert("Check submenus closed", () =>
                {
                    for (int j = 1; j <= i2 + 1; j++)
                    {
                        int menuIndex2 = j;
                        if (menus.GetSubMenu(menuIndex2).State == MenuState.Opened)
                            return false;
                    }

                    return true;
                });
            }
        }
        #endregion

        private void clickItem(int menuIndex, int itemIndex)
        {
            inputManager.MoveMouseTo(menus.GetSubStructure(menuIndex).GetMenuItem(itemIndex));
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

        private class ManualInputManager : PassThroughInputManager
        {
            private readonly ManualInputHandler handler;

            public ManualInputManager()
            {
                AddHandler(handler = new ManualInputHandler());
            }

            public void MoveMouseTo(Drawable drawable) => MoveMouseTo(drawable.ToScreenSpace(drawable.LayoutRectangle.Centre));
            public void MoveMouseTo(Vector2 position) => handler.MoveMouseTo(position);
            public void Click(MouseButton button) => handler.Click(button);
        }

        private class ManualInputHandler : InputHandler
        {
            private Vector2 lastMousePosition;

            public void MoveMouseTo(Vector2 position)
            {
                PendingStates.Enqueue(new InputState { Mouse = new MouseState { Position = position } });
                lastMousePosition = position;
            }

            public void Click(MouseButton button)
            {
                var mouseState = new MouseState { Position = lastMousePosition };
                mouseState.SetPressed(button, true);

                PendingStates.Enqueue(new InputState { Mouse = mouseState });

                mouseState = (MouseState)mouseState.Clone();
                mouseState.SetPressed(button, false);

                PendingStates.Enqueue(new InputState { Mouse = mouseState });
            }

            public override bool Initialize(GameHost host) => true;
            public override bool IsActive => true;
            public override int Priority => 0;
        }

        private class MenuStructure
        {
            private readonly Menu menu;

            public MenuStructure(Menu menu)
            {
                this.menu = menu;
            }

            public Drawable GetMenuItem(int index)
            {
                var contents = (CompositeDrawable)menu.InternalChildren[0];
                var contentContainer = (CompositeDrawable)contents.InternalChildren[1];
                var itemsContainer = (CompositeDrawable)((CompositeDrawable)contentContainer.InternalChildren[0]).InternalChildren[0];

                return itemsContainer.InternalChildren[index];
            }

            public MenuStructure GetSubStructure(int index) => new MenuStructure(GetSubMenu(index));

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
        }
    }
}
