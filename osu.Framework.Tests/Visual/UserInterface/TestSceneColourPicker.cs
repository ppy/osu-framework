// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Testing;

namespace osu.Framework.Tests.Visual.UserInterface
{
    public class TestSceneColourPicker : FrameworkTestScene
    {
        [Test]
        public void TestExternalColourSetAfterCreation()
        {
            ColourPicker colourPicker = null;

            AddStep("create picker", () => Child = colourPicker = new BasicColourPicker());
            AddStep("set colour externally", () => colourPicker.Current.Value = Colour4.Goldenrod);
            AddAssert("colour is correct", () => colourPicker.Current.Value == Colour4.Goldenrod);
        }

        [Test]
        public void TestExternalColourSetAtCreation()
        {
            ColourPicker colourPicker = null;

            AddStep("create picker", () => Child = colourPicker = new BasicColourPicker
            {
                Current = { Value = Colour4.Goldenrod }
            });
            AddAssert("colour is correct", () => colourPicker.Current.Value == Colour4.Goldenrod);
        }

        [Test]
        public void TestExternalHSVChange()
        {
            const float hue = 0.34f;
            const float saturation = 0.46f;
            const float value = 0.84f;

            ColourPicker colourPicker = null;

            AddStep("create picker", () => Child = colourPicker = new BasicColourPicker
            {
                Current = { Value = Colour4.Goldenrod }
            });
            AddStep("hide picker", () => colourPicker.Hide());
            AddStep("set HSV manually", () =>
            {
                var saturationValueControl = this.ChildrenOfType<HSVColourPicker.SaturationValueSelector>().Single();

                saturationValueControl.Hue.Value = hue;
                saturationValueControl.Saturation.Value = saturation;
                saturationValueControl.Value.Value = value;
            });

            AddUntilStep("colour is correct", () => colourPicker.Current.Value == Colour4.FromHSV(hue, saturation, value));
        }
    }
}
