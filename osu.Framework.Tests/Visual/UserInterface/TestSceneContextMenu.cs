// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.MathUtils;
using osu.Framework.Testing;
using osuTK;
using osuTK.Graphics;
using osuTK.Input;

namespace osu.Framework.Tests.Visual.UserInterface
{
    public class TestSceneContextMenu : ManualInputManagerTestScene
    {
        protected override Container<Drawable> Content => contextMenuContainer ?? base.Content;

        private readonly TestContextMenuContainer contextMenuContainer;

        public TestSceneContextMenu()
        {
            base.Content.Add(contextMenuContainer = new TestContextMenuContainer { RelativeSizeAxes = Axes.Both });
        }

        [SetUp]
        public void Setup() => Schedule(Clear);

        [Test]
        public void TestMenuOpenedOnClick()
        {
            Drawable box = null;

            addBoxStep(b => box = b, 1);
            clickBoxStep(() => box);

            assertMenuState(true);
        }

        [Test]
        public void TestMenuClosedOnClickOutside()
        {
            Drawable box = null;

            addBoxStep(b => box = b, 1);
            clickBoxStep(() => box);

            clickOutsideStep();
            assertMenuState(false);
        }

        [Test]
        public void TestMenuTransferredToNewTarget()
        {
            Drawable box1 = null;
            Drawable box2 = null;

            addBoxStep(b =>
            {
                box1 = b.With(d =>
                {
                    d.X = -100;
                    d.Colour = Color4.Green;
                });
            }, 1);
            addBoxStep(b =>
            {
                box2 = b.With(d =>
                {
                    d.X = 100;
                    d.Colour = Color4.Red;
                });
            }, 1);

            clickBoxStep(() => box1);
            clickBoxStep(() => box2);

            assertMenuState(true);
            assertMenuInCentre(() => box2);
        }

        [Test]
        public void TestMenuHiddenWhenTargetHidden()
        {
            Drawable box = null;

            addBoxStep(b => box = b, 1);
            clickBoxStep(() => box);

            AddStep("hide box", () => box.Hide());
            assertMenuState(false);
        }

        [Test]
        public void TestMenuTracksMovement()
        {
            Drawable box = null;

            addBoxStep(b => box = b, 1);
            clickBoxStep(() => box);

            AddStep("move box", () => box.X += 100);
            assertMenuInCentre(() => box);
        }

        [TestCase(Anchor.TopLeft)]
        [TestCase(Anchor.TopCentre)]
        [TestCase(Anchor.TopRight)]
        [TestCase(Anchor.CentreLeft)]
        [TestCase(Anchor.CentreRight)]
        [TestCase(Anchor.BottomLeft)]
        [TestCase(Anchor.BottomCentre)]
        [TestCase(Anchor.BottomRight)]
        public void TestMenuOnScreenWhenTargetPartlyOffScreen(Anchor anchor)
        {
            Drawable box = null;

            addBoxStep(b => box = b, 5);
            clickBoxStep(() => box);

            AddStep($"move box to {anchor.ToString()}", () =>
            {
                box.Anchor = anchor;
                box.X -= 5;
                box.Y -= 5;
            });

            assertMenuOnScreen(true);
        }

        [TestCase(Anchor.TopLeft)]
        [TestCase(Anchor.TopCentre)]
        [TestCase(Anchor.TopRight)]
        [TestCase(Anchor.CentreLeft)]
        [TestCase(Anchor.CentreRight)]
        [TestCase(Anchor.BottomLeft)]
        [TestCase(Anchor.BottomCentre)]
        [TestCase(Anchor.BottomRight)]
        public void TestMenuNotOnScreenWhenTargetSignificantlyOffScreen(Anchor anchor)
        {
            Drawable box = null;

            addBoxStep(b => box = b, 5);
            clickBoxStep(() => box);

            AddStep($"move box to {anchor.ToString()}", () =>
            {
                box.Anchor = anchor;

                if (anchor.HasFlag(Anchor.x0))
                    box.X -= contextMenuContainer.CurrentMenu.DrawWidth + 10;
                else if (anchor.HasFlag(Anchor.x2))
                    box.X += 10;

                if (anchor.HasFlag(Anchor.y0))
                    box.Y -= contextMenuContainer.CurrentMenu.DrawHeight + 10;
                else if (anchor.HasFlag(Anchor.y2))
                    box.Y += 10;
            });

            assertMenuOnScreen(false);
        }

        [Test]
        public void TestReturnNullInNestedDrawableOpensParentMenu()
        {
            Drawable box2 = null;

            addBoxStep(_ => { }, 2);
            addBoxStep(b => box2 = b, null);

            clickBoxStep(() => box2);
            assertMenuState(true);
            assertMenuItems(2);
        }

        [Test]
        public void TestReturnEmptyInNestedDrawableBlocksMenuOpening()
        {
            Drawable box2 = null;

            addBoxStep(_ => { }, 2);
            addBoxStep(b => box2 = b);

            clickBoxStep(() => box2);
            assertMenuState(false);
        }

        private void clickBoxStep(Func<Drawable> getBoxFunc)
        {
            AddStep("right-click box", () =>
            {
                InputManager.MoveMouseTo(getBoxFunc());
                InputManager.Click(MouseButton.Right);
            });
        }

        private void clickOutsideStep()
        {
            AddStep("click outside", () =>
            {
                InputManager.MoveMouseTo(InputManager.ScreenSpaceDrawQuad.TopLeft);
                InputManager.Click(MouseButton.Right);
            });
        }

        private void addBoxStep(Action<Drawable> boxFunc, int actionCount) => addBoxStep(boxFunc, Enumerable.Repeat(new Action(() => { }), actionCount).ToArray());

        private void addBoxStep(Action<Drawable> boxFunc, params Action[] actions)
        {
            AddStep("add box", () =>
            {
                var box = new BoxWithContextMenu(actions)
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Size = new Vector2(200),
                };

                Add(box);
                boxFunc?.Invoke(box);
            });
        }

        private void assertMenuState(bool opened)
            => AddAssert($"menu {(opened ? "opened" : "closed")}", () => (contextMenuContainer.CurrentMenu?.State == MenuState.Open) == opened);

        private void assertMenuInCentre(Func<Drawable> getBoxFunc)
            => AddAssert("menu in centre of box", () => Precision.AlmostEquals(contextMenuContainer.CurrentMenu.ScreenSpaceDrawQuad.TopLeft, getBoxFunc().ScreenSpaceDrawQuad.Centre));

        private void assertMenuOnScreen(bool expected) => AddAssert($"menu {(expected ? "on" : "off")} screen", () =>
        {
            var inputQuad = InputManager.ScreenSpaceDrawQuad;
            var menuQuad = contextMenuContainer.CurrentMenu.ScreenSpaceDrawQuad;

            bool result = inputQuad.Contains(menuQuad.TopLeft + new Vector2(1, 1))
                          && inputQuad.Contains(menuQuad.TopRight + new Vector2(-1, 1))
                          && inputQuad.Contains(menuQuad.BottomLeft + new Vector2(1, -1))
                          && inputQuad.Contains(menuQuad.BottomRight + new Vector2(-1, -1));

            return result == expected;
        });

        private void assertMenuItems(int expectedCount) => AddAssert($"menu contains {expectedCount} item(s)", () => contextMenuContainer.CurrentMenu.Items.Count == expectedCount);

        private class BoxWithContextMenu : Box, IHasContextMenu
        {
            private readonly Action[] actions;

            public BoxWithContextMenu(Action[] actions)
            {
                this.actions = actions;
            }

            public MenuItem[] ContextMenuItems => actions?.Select((a, i) => new MenuItem($"Item {i}", a)).ToArray();
        }

        private class TestContextMenuContainer : BasicContextMenuContainer
        {
            public Menu CurrentMenu { get; private set; }

            protected override Menu CreateMenu() => CurrentMenu = base.CreateMenu();
        }
    }
}
