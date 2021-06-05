// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Testing;

namespace osu.Framework.Tests.Visual.UserInterface
{
    public class TestSceneHexColourPicker : FrameworkTestScene
    {
        private TestHexColourPicker hexColourPicker;

        [SetUpSteps]
        public void SetUpSteps()
        {
            AddStep("create picker", () => Child = hexColourPicker = new TestHexColourPicker());
        }

        private class TestHexColourPicker : BasicHexColourPicker
        {
            public TextBox HexCodeTextBox => this.ChildrenOfType<TextBox>().Single();
            public ColourPreview Preview => this.ChildrenOfType<ColourPreview>().Single();
        }
    }
}
