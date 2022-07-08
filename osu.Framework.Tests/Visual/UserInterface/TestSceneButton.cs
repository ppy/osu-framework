// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Testing;
using osuTK;
using osuTK.Input;

namespace osu.Framework.Tests.Visual.UserInterface
{
    public class TestSceneButton : ManualInputManagerTestScene
    {
        private int clickCount;
        private readonly BasicButton button;

        public TestSceneButton()
        {
            Add(button = new BasicButton
            {
                Anchor = Anchor.TopLeft,
                Origin = Anchor.TopLeft,
                Text = "this is a button",
                Size = new Vector2(200, 40),
                Margin = new MarginPadding(10),
                FlashColour = FrameworkColour.Green,
                Action = () => clickCount++
            });
        }

        [SetUp]
        public new void SetUp() => Schedule(() =>
        {
            clickCount = 0;
            button.Enabled.Value = true;
        });

        [Test]
        public void Button()
        {
            AddStep("click button", () =>
            {
                InputManager.MoveMouseTo(button.ScreenSpaceDrawQuad.Centre);
                InputManager.Click(MouseButton.Left);
            });
            AddAssert("action was executed", () => clickCount == 1);
        }

        [Test]
        public void DisabledButton()
        {
            AddStep("disable button", () => button.Enabled.Value = false);
            AddStep("click button", () =>
            {
                InputManager.MoveMouseTo(button.ScreenSpaceDrawQuad.Centre);
                InputManager.Click(MouseButton.Left);
            });
            AddAssert("action was not executed", () => clickCount == 0);
        }
    }
}
