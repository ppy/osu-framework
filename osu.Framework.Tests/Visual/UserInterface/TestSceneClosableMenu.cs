// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Events;
using osu.Framework.Testing;
using osuTK.Input;

namespace osu.Framework.Tests.Visual.UserInterface
{
    public partial class TestSceneClosableMenu : MenuTestScene
    {
        [SetUpSteps]
        public void SetUpSteps()
        {
            CreateMenu(() => new AnimatedMenu(Direction.Vertical)
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                State = MenuState.Open,
                Items = new[]
                {
                    new MenuItem("Item #1")
                    {
                        Items = new[]
                        {
                            new MenuItem("Sub-item #1"),
                            new MenuItem("Sub-item #2", () => { }),
                        }
                    },
                    new MenuItem("Item #2")
                    {
                        Items = new[]
                        {
                            new MenuItem("Sub-item #1"),
                            new MenuItem("Sub-item #2", () => { }),
                        }
                    },
                }
            });
        }

        [Test]
        public void TestClickItemWithoutActionDoesNotCloseMenus()
        {
            AddStep("click item", () => ClickItem(0, 0));
            AddStep("click item", () => ClickItem(1, 0));
            AddAssert("no menus closed", () =>
            {
                for (int i = 1; i >= 0; --i)
                {
                    if (Menus.GetSubMenu(i).State == MenuState.Closed)
                        return false;
                }

                return true;
            });
        }

        [Test]
        public void TestClickItemWithActionAssignedDuringNavigationClosesMenus()
        {
            AddStep("click item", () => ClickItem(0, 0));
            AddStep("hover item", () => InputManager.MoveMouseTo(Menus.GetSubStructure(1).GetMenuItems()[0]));
            AddStep("assign action", () => Menus.GetSubStructure(1).GetMenuItems().Cast<Menu.DrawableMenuItem>().First().Item.Action.Value = () => { });
            AddStep("click item", () => InputManager.Click(MouseButton.Left));
            AddAssert("all menus closed", () =>
            {
                for (int i = 1; i >= 0; --i)
                {
                    if (Menus.GetSubMenu(i).State == MenuState.Open)
                        return false;
                }

                return true;
            });
        }

        [Test]
        public void TestClickItemWithActionClosesMenus()
        {
            AddStep("click item", () => ClickItem(0, 0));
            AddStep("click item", () => ClickItem(1, 1));
            AddAssert("all menus closed", () =>
            {
                for (int i = 1; i >= 0; --i)
                {
                    if (Menus.GetSubMenu(i).State == MenuState.Open)
                        return false;
                }

                return true;
            });
        }

        [Test]
        public void TestMenuIgnoresEscapeWhenClosed()
        {
            AnimatedMenu menu = null;

            AddStep("find menu", () => menu = (AnimatedMenu)Menus.GetSubMenu(0));
            AddStep("press escape", () => InputManager.Key(Key.Escape));
            AddAssert("press handled", () => menu.PressBlocked);
            AddStep("reset flag", () => menu.PressBlocked = false);
            AddStep("press escape again", () => InputManager.Key(Key.Escape));
            AddAssert("press not handled", () => !menu.PressBlocked);
        }

        [Test]
        public void TestMenuBlocksInputUnderneathIt()
        {
            bool itemClicked = false;
            bool actionReceived = false;

            AddStep("set item action", () => Menus.GetSubMenu(0).Items[0].Items[0].Action.Value = () => itemClicked = true);
            AddStep("add mouse handler", () => Add(new MouseHandlingLayer
            {
                Action = () => actionReceived = true,
                Depth = 1,
            }));

            AddStep("click item", () => ClickItem(0, 0));
            AddStep("click item", () => ClickItem(1, 0));
            AddAssert("menu item activated", () => itemClicked);
            AddAssert("mouse handler not activated", () => !actionReceived);
        }

        private partial class MouseHandlingLayer : Drawable
        {
            public Action Action { get; set; }

            public MouseHandlingLayer()
            {
                RelativeSizeAxes = Axes.Both;
            }

            protected override bool OnMouseDown(MouseDownEvent e)
            {
                Action?.Invoke();
                return base.OnMouseDown(e);
            }
        }

        private partial class AnimatedMenu : BasicMenu
        {
            public bool PressBlocked { get; set; }

            public AnimatedMenu(Direction direction)
                : base(direction)
            {
            }

            protected override bool OnKeyDown(KeyDownEvent e)
            {
                return PressBlocked = base.OnKeyDown(e);
            }

            protected override void AnimateOpen() => this.FadeIn(500);

            protected override void AnimateClose() => this.FadeOut(5000); // Ensure escape is pressed while menu is still fading

            protected override Menu CreateSubMenu() => new AnimatedMenu(Direction);
        }
    }
}
