// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.UserInterface;

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
    }
}
