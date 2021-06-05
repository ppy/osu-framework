// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Testing;
using osu.Framework.Utils;
using osuTK;
using osuTK.Input;

namespace osu.Framework.Tests.Visual.UserInterface
{
    public class TestSceneHSVColourPicker : ManualInputManagerTestScene
    {
        private BasicHSVColourPicker colourPicker;

        [SetUpSteps]
        public void SetUpSteps()
        {
            AddStep("create picker", () => Child = colourPicker = new BasicHSVColourPicker());
        }

        [Test]
        public void HueSelectorInput()
        {
            HSVColourPicker.HueSelector selector = null;

            AddStep("retrieve selector", () => selector = colourPicker.ChildrenOfType<HSVColourPicker.HueSelector>().Single());
            AddAssert("initial hue is 0", () => selector.Hue.Value == 0);

            AddStep("click selector centre", () =>
            {
                InputManager.MoveMouseTo(selector.ScreenSpaceDrawQuad.Centre);
                InputManager.Click(MouseButton.Left);
            });
            AddAssert("hue is 0.5", () => Precision.AlmostEquals(0.5f, selector.Hue.Value));

            AddStep("click right edge of selector", () =>
            {
                InputManager.MoveMouseTo(new Vector2(selector.ScreenSpaceDrawQuad.TopRight.X - 1, selector.ScreenSpaceDrawQuad.Centre.Y));
                InputManager.Click(MouseButton.Left);
            });
            AddAssert("hue is 1", () => Precision.AlmostEquals(1f, selector.Hue.Value, 0.005));

            AddStep("drag back to start", () =>
            {
                InputManager.PressButton(MouseButton.Left);
                InputManager.MoveMouseTo(new Vector2(selector.ScreenSpaceDrawQuad.TopLeft.X + 1, selector.ScreenSpaceDrawQuad.Centre.Y));
                InputManager.ReleaseButton(MouseButton.Left);
            });
            AddAssert("hue is 0", () => Precision.AlmostEquals(0f, selector.Hue.Value, 0.005));
        }
    }
}
