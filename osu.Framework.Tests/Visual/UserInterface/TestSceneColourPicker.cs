// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Testing;

namespace osu.Framework.Tests.Visual.UserInterface
{
    public class TestSceneColourPicker : FrameworkTestScene
    {
        [SetUpSteps]
        public void SetUpSteps()
        {
            ColourPicker colourPicker = null;

            AddStep("create picker", () => Child = colourPicker = new BasicColourPicker());
            AddStep("set colour externally", () => colourPicker.Current.Value = Colour4.CornflowerBlue);
        }
    }
}
