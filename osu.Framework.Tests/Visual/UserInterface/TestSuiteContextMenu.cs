// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Testing;
using osuTK;
using osuTK.Input;

namespace osu.Framework.Tests.Visual.UserInterface
{
    public class TestSuiteContextMenu : ManualInputManagerTestSuite<TestSceneContextMenu>
    {
        private const int duration = 1000;

        public override IReadOnlyList<Type> RequiredTypes => new[]
        {
            typeof(Menu),
            typeof(BasicMenu),
            typeof(ContextMenuContainer),
            typeof(BasicContextMenuContainer)
        };

        protected override void LoadComplete()
        {
            base.LoadComplete();

            const float movement_amount = 500;

            // Move box along a square trajectory
            TestScene.MovingBox.MoveTo(new Vector2(movement_amount, 0), duration)
                     .Then().MoveTo(new Vector2(-movement_amount, 0), duration * 2)
                     .Then().MoveTo(Vector2.Zero, duration)
                     .Loop();
        }

        [Test]
        public void TestStaysOnScreen()
        {
            foreach (var c in TestScene.ContextContainer)
                testDrawableCornerClicks(c, c == TestScene.MovingBox);
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
                AddAssert("check completely on screen", () => isTrackingTargetCorrectly(TestScene.ContextContainer.CurrentMenu, target));
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
    }
}
