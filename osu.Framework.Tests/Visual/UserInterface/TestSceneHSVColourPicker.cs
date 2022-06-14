// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Testing;
using osu.Framework.Utils;
using osuTK;
using osuTK.Graphics;
using osuTK.Input;

namespace osu.Framework.Tests.Visual.UserInterface
{
    public class TestSceneHSVColourPicker : ManualInputManagerTestScene
    {
        private TestHSVColourPicker colourPicker;

        [SetUpSteps]
        public void SetUpSteps()
        {
            AddStep("create picker", () => Child = colourPicker = new TestHSVColourPicker());
        }

        [Test]
        public void HueSelectorInput()
        {
            assertHue(0);

            AddStep("click selector centre", () =>
            {
                InputManager.MoveMouseTo(colourPicker.HueControl.ScreenSpaceDrawQuad.Centre);
                InputManager.Click(MouseButton.Left);
            });
            assertHue(0.5f);

            AddStep("click right edge of selector", () =>
            {
                InputManager.MoveMouseTo(new Vector2(colourPicker.HueControl.ScreenSpaceDrawQuad.TopRight.X - 1, colourPicker.HueControl.ScreenSpaceDrawQuad.Centre.Y));
                InputManager.Click(MouseButton.Left);
            });
            assertHue(1);

            AddStep("drag back to start", () =>
            {
                InputManager.PressButton(MouseButton.Left);
                InputManager.MoveMouseTo(new Vector2(colourPicker.HueControl.ScreenSpaceDrawQuad.TopLeft.X + 1, colourPicker.HueControl.ScreenSpaceDrawQuad.Centre.Y));
                InputManager.ReleaseButton(MouseButton.Left);
            });
            assertHue(0);
        }

        [Test]
        public void SaturationValueSelectorInput()
        {
            AddStep("set initial colour", () => colourPicker.Current.Value = Color4.Red);
            assertSaturationAndValue(1, 1, 0);

            AddStep("click top left corner", () =>
            {
                InputManager.MoveMouseTo(colourPicker.SaturationValueControl.ScreenSpaceDrawQuad.TopLeft + new Vector2(1));
                InputManager.Click(MouseButton.Left);
            });
            assertSaturationAndValue(0, 1);

            AddStep("drag to center", () =>
            {
                InputManager.PressButton(MouseButton.Left);
                InputManager.MoveMouseTo(colourPicker.SaturationValueControl.ScreenSpaceDrawQuad.Centre);
                InputManager.ReleaseButton(MouseButton.Left);
            });
            assertSaturationAndValue(0.5f, 0.5f);

            AddStep("click bottom left corner", () =>
            {
                InputManager.MoveMouseTo(colourPicker.SaturationValueControl.ScreenSpaceDrawQuad.BottomLeft + new Vector2(1, -1));
                InputManager.Click(MouseButton.Left);
            });
            assertSaturationAndValue(0, 0);

            AddStep("click bottom right corner", () =>
            {
                InputManager.MoveMouseTo(colourPicker.SaturationValueControl.ScreenSpaceDrawQuad.BottomRight - new Vector2(1));
                InputManager.Click(MouseButton.Left);
            });
            assertSaturationAndValue(1, 0);

            AddStep("change hue", () =>
            {
                InputManager.MoveMouseTo(colourPicker.HueControl.ScreenSpaceDrawQuad.Centre);
                InputManager.Click(MouseButton.Left);
            });
            assertHue(0.5f);
        }

        [Test]
        public void TestExternalChange()
        {
            const float hue = 0.34f;
            const float saturation = 0.46f;
            const float value = 0.84f;
            Colour4 colour = Colour4.FromHSV(hue, saturation, value);

            AddStep("set colour", () => colourPicker.Current.Value = colour);

            assertHue(hue);
            assertSaturationAndValue(saturation, value);
        }

        [Test]
        public void TestExternalChangeWhileNotPresent()
        {
            const float hue = 0.34f;
            const float saturation = 0.46f;
            const float value = 0.84f;

            AddStep("hide picker", () => colourPicker.Hide());
            AddStep("set HSV manually", () =>
            {
                colourPicker.SaturationValueControl.Hue.Value = hue;
                colourPicker.SaturationValueControl.Saturation.Value = saturation;
                colourPicker.SaturationValueControl.Value.Value = value;
            });

            AddUntilStep("colour is correct", () => colourPicker.Current.Value == Colour4.FromHSV(hue, saturation, value));
        }

        [Test]
        public void TestHueUnchangedIfSaturationAlmostZero()
        {
            AddStep("change colour", () => colourPicker.Current.Value = Colour4.FromHSV(0.5f, 0.5f, 0.5f));
            AddStep("set saturation to 0", () => colourPicker.SaturationValueControl.Saturation.Value = 0);
            AddAssert("hue is unchanged", () => colourPicker.HueControl.Hue.Value == 0.5f);
        }

        [Test]
        public void TestHueDoesNotWrapAround()
        {
            // the hue of 1 is special, because it's equivalent to hue of 0.
            // we want to make sure there is never a jump from 1 to 0, since it doesn't actually do anything.
            AddStep("set hue to 1", () => colourPicker.HueControl.Hue.Value = 1);
            AddStep("click saturation value control", () =>
            {
                InputManager.MoveMouseTo(colourPicker.SaturationValueControl.ScreenSpaceDrawQuad.Centre);
                InputManager.Click(MouseButton.Left);
            });
            assertHue(1, 0);

            AddStep("set hue to 1", () => colourPicker.HueControl.Hue.Value = 1);
            AddStep("set colour externally", () => colourPicker.Current.Value = Colour4.Red);
            assertHue(1, 0);
        }

        private void assertHue(float hue, float tolerance = 0.005f)
        {
            AddAssert($"hue selector has {hue}", () => Precision.AlmostEquals(colourPicker.HueControl.Hue.Value, hue, tolerance));
            AddAssert($"saturation/value selector has {hue}", () => Precision.AlmostEquals(colourPicker.SaturationValueControl.Hue.Value, hue, tolerance));
        }

        private void assertSaturationAndValue(float saturation, float value, float tolerance = 0.005f)
        {
            AddAssert($"saturation is {saturation}", () => Precision.AlmostEquals(colourPicker.SaturationValueControl.Saturation.Value, saturation, tolerance));
            AddAssert($"value is {value}", () => Precision.AlmostEquals(colourPicker.SaturationValueControl.Value.Value, value, tolerance));
        }

        private class TestHSVColourPicker : BasicHSVColourPicker
        {
            public HueSelector HueControl => this.ChildrenOfType<HueSelector>().Single();
            public SaturationValueSelector SaturationValueControl => this.ChildrenOfType<SaturationValueSelector>().Single();
        }
    }
}
