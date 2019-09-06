// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Testing;
using osuTK;
using osuTK.Graphics;
using osuTK.Input;

namespace osu.Framework.Tests.Visual.UserInterface
{
    public class TestSceneContextMenu : ManualInputManagerTestScene
    {
        private const int duration = 1000;

        private readonly ContextMenuBox movingBox;

        private readonly TestContextMenuContainer contextContainer;

        public override IReadOnlyList<Type> RequiredTypes => new[]
        {
            typeof(Menu),
            typeof(BasicMenu),
            typeof(ContextMenuContainer),
            typeof(BasicContextMenuContainer)
        };

        private ContextMenuBox makeBox(Anchor anchor) =>
            new ContextMenuBox
            {
                Size = new Vector2(200),
                Anchor = anchor,
                Origin = anchor,
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.Blue,
                    }
                }
            };

        public TestSceneContextMenu()
        {
            Add(contextContainer = new TestContextMenuContainer
            {
                RelativeSizeAxes = Axes.Both,
                Children = new[]
                {
                    makeBox(Anchor.TopLeft),
                    makeBox(Anchor.TopRight),
                    makeBox(Anchor.BottomLeft),
                    makeBox(Anchor.BottomRight),
                    movingBox = makeBox(Anchor.Centre),
                }
            });
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            const float movement_amount = 500;

            // Move box along a square trajectory
            movingBox.MoveTo(new Vector2(movement_amount, 0), duration)
                     .Then().MoveTo(new Vector2(-movement_amount, 0), duration * 2)
                     .Then().MoveTo(Vector2.Zero, duration)
                     .Loop();
        }

        public class TestContextMenuContainer : BasicContextMenuContainer
        {
            public Menu CurrentMenu;

            protected override Menu CreateMenu() => CurrentMenu = base.CreateMenu();
        }

        [Test]
        public void TestStaysOnScreen()
        {
            foreach (var c in contextContainer)
                testDrawableCornerClicks(c, c == movingBox);
        }

        private void testDrawableCornerClicks(Drawable box, bool testManyTimes)
        {
            const float lenience = 5;

            testPositionalClick(box, () => box.ScreenSpaceDrawQuad.TopLeft + new Vector2(lenience, lenience), testManyTimes);
            testPositionalClick(box, () => box.ScreenSpaceDrawQuad.TopRight + new Vector2(-lenience, lenience), testManyTimes);
            testPositionalClick(box, () => box.ScreenSpaceDrawQuad.BottomLeft + new Vector2(lenience, -lenience), testManyTimes);
            testPositionalClick(box, () => box.ScreenSpaceDrawQuad.BottomRight + new Vector2(-lenience, -lenience), testManyTimes);
            testPositionalClick(box, () => box.ScreenSpaceDrawQuad.Centre, testManyTimes);
        }

        private void testPositionalClick(Drawable target, Func<Vector2> pos, bool testManyTimes)
        {
            AddStep("click position", () =>
            {
                InputManager.MoveMouseTo(pos());
                InputManager.Click(MouseButton.Right);
            });

            for (int i = 0; i < (testManyTimes ? 10 : 1); i++)
                AddAssert("check completely on screen", () => isTrackingTargetCorrectly(contextContainer.CurrentMenu, target));
        }

        private bool isTrackingTargetCorrectly(Drawable menu, Drawable target)
        {
            bool targetOnScreen = isOnScreen(target);
            bool menuOnScreen = isOnScreen(menu);

            return !targetOnScreen || menuOnScreen;
        }

        private bool isOnScreen(Drawable checkDrawable)
        {
            var inputQuad = InputManager.ScreenSpaceDrawQuad;
            var menuQuad = checkDrawable.ScreenSpaceDrawQuad;

            return inputQuad.Contains(menuQuad.TopLeft + new Vector2(1, 1))
                   && inputQuad.Contains(menuQuad.TopRight + new Vector2(-1, 1))
                   && inputQuad.Contains(menuQuad.BottomLeft + new Vector2(1, -1))
                   && inputQuad.Contains(menuQuad.BottomRight + new Vector2(-1, -1));
        }

        private class ContextMenuBox : Container, IHasContextMenu
        {
            public MenuItem[] ContextMenuItems => new[]
            {
                new MenuItem(@"Change width", () => this.ResizeWidthTo(Width * 2, 100, Easing.OutQuint)),
                new MenuItem(@"Change height", () => this.ResizeHeightTo(Height * 2, 100, Easing.OutQuint)),
                new MenuItem(@"Change width back", () => this.ResizeWidthTo(Width / 2, 100, Easing.OutQuint)),
                new MenuItem(@"Change height back", () => this.ResizeHeightTo(Height / 2, 100, Easing.OutQuint)),
            };
        }
    }
}
