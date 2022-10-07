// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Testing;
using osuTK;
using osuTK.Graphics;
using osuTK.Input;

namespace osu.Framework.Tests.Visual.Input
{
    public class TestSceneClickLenience : ManualInputManagerTestScene
    {
        private ClickBox box;

        private Drawable createClickBox(TestType type)
        {
            switch (type)
            {
                case TestType.NonBlockingScroll:
                    return new NonBlockingScroll
                    {
                        RelativeSizeAxes = Axes.Both,
                        Child = box = new ClickBox()
                    };

                case TestType.Scroll:
                    return new BasicScrollContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        Child = box = new ClickBox()
                    };

                default:
                    return box = new ClickBox();
            }
        }

        [TestCase(TestType.Direct)]
        [TestCase(TestType.Scroll)]
        [TestCase(TestType.NonBlockingScroll)]
        public void TestBasicClick(TestType type)
        {
            AddStep("create button", () => Child = createClickBox(type));

            AddStep("move to button", () => InputManager.MoveMouseTo(box));
            AddStep("click", () => InputManager.Click(MouseButton.Left));

            checkClicked(true);
        }

        [TestCase(TestType.Direct)]
        [TestCase(TestType.Scroll)]
        [TestCase(TestType.NonBlockingScroll)]
        public void TestVerticalDragOnButton(TestType type)
        {
            AddStep("create button", () => Child = createClickBox(type));

            AddStep("move to TopLeft", () => InputManager.MoveMouseTo(box.ScreenSpaceDrawQuad.AABBFloat.Shrink(10).TopLeft));
            AddStep("mouse down", () => InputManager.PressButton(MouseButton.Left));
            AddStep("move to BottomLeft", () => InputManager.MoveMouseTo(box.ScreenSpaceDrawQuad.AABBFloat.Shrink(10).BottomLeft));
            AddStep("mouse up", () => InputManager.ReleaseButton(MouseButton.Left));

            checkClicked(type != TestType.Scroll);
        }

        [TestCase(TestType.Direct)]
        [TestCase(TestType.Scroll)]
        [TestCase(TestType.NonBlockingScroll)]
        public void TestHorizontalDragOnButton(TestType type)
        {
            AddStep("create button", () => Child = createClickBox(type));

            AddStep("move to TopLeft", () => InputManager.MoveMouseTo(box.ScreenSpaceDrawQuad.AABBFloat.Shrink(10).TopLeft));
            AddStep("mouse down", () => InputManager.PressButton(MouseButton.Left));
            AddStep("move to TopRight", () => InputManager.MoveMouseTo(box.ScreenSpaceDrawQuad.AABBFloat.Shrink(10).TopRight + new Vector2(0, 5)));
            AddStep("mouse up", () => InputManager.ReleaseButton(MouseButton.Left));

            checkClicked(true);
        }

        [TestCase(TestType.Direct)]
        [TestCase(TestType.Scroll)]
        [TestCase(TestType.NonBlockingScroll)]
        public void TestHorizontalDragOut(TestType type)
        {
            AddStep("create button", () => Child = createClickBox(type));

            AddStep("move to TopLeft", () => InputManager.MoveMouseTo(box.ScreenSpaceDrawQuad.AABBFloat.Shrink(10).TopLeft));
            AddStep("mouse down", () => InputManager.PressButton(MouseButton.Left));
            AddStep("move out", () => InputManager.MoveMouseTo(box.ScreenSpaceDrawQuad.AABBFloat.TopLeft - new Vector2(10, 0)));
            AddStep("mouse up", () => InputManager.ReleaseButton(MouseButton.Left));

            checkClicked(false);
        }

        [TestCase(TestType.Direct)]
        [TestCase(TestType.Scroll)]
        [TestCase(TestType.NonBlockingScroll)]
        public void TestHorizontalDragOutIn(TestType type)
        {
            AddStep("create button", () => Child = createClickBox(type));

            AddStep("move to TopLeft", () => InputManager.MoveMouseTo(box.ScreenSpaceDrawQuad.AABBFloat.Shrink(10).TopLeft));
            AddStep("mouse down", () => InputManager.PressButton(MouseButton.Left));
            AddStep("move out", () => InputManager.MoveMouseTo(box.ScreenSpaceDrawQuad.AABBFloat.TopLeft - new Vector2(10, 0)));
            AddStep("move back in", () => InputManager.MoveMouseTo(box.ScreenSpaceDrawQuad.AABBFloat.Shrink(10).TopLeft));
            AddStep("mouse up", () => InputManager.ReleaseButton(MouseButton.Left));

            checkClicked(true);
        }

        private void checkClicked(bool clicked) => AddAssert($"button {(clicked ? "clicked" : "not clicked")}", () => box.Clicked == clicked);

        public class ClickBox : BasicButton
        {
            public bool Clicked;

            public ClickBox()
            {
                Text = "Click me!";

                Anchor = Anchor.Centre;
                Origin = Anchor.Centre;

                Action = () =>
                {
                    Clicked = true;
                    Text = "Ouch!";
                    this.ScaleTo(0.95f).Then().ScaleTo(1, 1000, Easing.In);
                    this.FlashColour(Color4.Red, 1000, Easing.InQuint);
                };

                RelativeSizeAxes = Axes.X;
                Height = 100;
            }
        }

        public class NonBlockingScroll : BasicScrollContainer
        {
            public override bool DragBlocksClick => false;
        }

        public enum TestType
        {
            Direct,
            Scroll,
            NonBlockingScroll
        }
    }
}
