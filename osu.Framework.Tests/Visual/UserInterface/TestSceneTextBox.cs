// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Testing;
using osuTK;

namespace osu.Framework.Tests.Visual.UserInterface
{
    public class TestSceneTextBox : TestScene
    {
        public FillFlowContainer TextBoxes { get; private set; }

        public TestSceneTextBox()
        {
            Schedule(() =>
            {
                Child = TextBoxes = new FillFlowContainer
                {
                    Direction = FillDirection.Vertical,
                    Spacing = new Vector2(0, 50),
                    Padding = new MarginPadding
                    {
                        Top = 50,
                    },
                    Anchor = Anchor.TopCentre,
                    Origin = Anchor.TopCentre,
                    RelativeSizeAxes = Axes.Both,
                    Size = new Vector2(0.9f, 1)
                };
            });
        }

        public class InsertableTextBox : BasicTextBox
        {
            public new void InsertString(string text) => base.InsertString(text);
        }

        public class NumberTextBox : BasicTextBox
        {
            protected override bool CanAddCharacter(char character) => char.IsNumber(character);
        }
    }
}
