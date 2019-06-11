// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.UserInterface;
using osuTK;

namespace osu.Framework.Tests.Visual.UserInterface
{
    public class TestSceneTextBox : FrameworkTestScene
    {
        public override IReadOnlyList<Type> RequiredTypes => new[]
        {
            typeof(BasicTextBox),
            typeof(TextBox),
            typeof(PasswordTextBox)
        };

        public TestSceneTextBox()
        {
            FillFlowContainer textBoxes = new FillFlowContainer
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

            Add(textBoxes);

            textBoxes.Add(new BasicTextBox
            {
                Size = new Vector2(100, 16),
                TabbableContentContainer = textBoxes
            });

            textBoxes.Add(new BasicTextBox
            {
                Text = @"Limited length",
                Size = new Vector2(200, 20),
                LengthLimit = 20,
                TabbableContentContainer = textBoxes
            });

            textBoxes.Add(new BasicTextBox
            {
                Text = @"Box with some more text",
                Size = new Vector2(500, 30),
                TabbableContentContainer = textBoxes
            });

            textBoxes.Add(new BasicTextBox
            {
                PlaceholderText = @"Placeholder text",
                Size = new Vector2(500, 30),
                TabbableContentContainer = textBoxes
            });

            textBoxes.Add(new BasicTextBox
            {
                Text = @"prefilled placeholder",
                PlaceholderText = @"Placeholder text",
                Size = new Vector2(500, 30),
                TabbableContentContainer = textBoxes
            });

            NumberTextBox numbers;
            textBoxes.Add(numbers = new NumberTextBox
            {
                PlaceholderText = @"Only numbers",
                Size = new Vector2(500, 30),
                TabbableContentContainer = textBoxes
            });

            textBoxes.Add(new BasicTextBox
            {
                Text = "Readonly textbox",
                Size = new Vector2(500, 30),
                ReadOnly = true,
                TabbableContentContainer = textBoxes
            });

            FillFlowContainer otherTextBoxes = new FillFlowContainer
            {
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 50),
                Padding = new MarginPadding
                {
                    Top = 50,
                    Left = 500
                },
                Anchor = Anchor.TopCentre,
                Origin = Anchor.TopCentre,
                RelativeSizeAxes = Axes.Both,
                Size = new Vector2(0.8f, 1)
            };

            otherTextBoxes.Add(new BasicTextBox
            {
                PlaceholderText = @"Textbox in separate container",
                Size = new Vector2(500, 30),
                TabbableContentContainer = otherTextBoxes
            });

            otherTextBoxes.Add(new PasswordTextBox
            {
                PlaceholderText = @"Password textbox",
                Text = "Secret ;)",
                Size = new Vector2(500, 30),
                TabbableContentContainer = otherTextBoxes
            });

            FillFlowContainer nestedTextBoxes = new FillFlowContainer
            {
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 50),
                Margin = new MarginPadding { Left = 50 },
                RelativeSizeAxes = Axes.Both,
                Size = new Vector2(0.8f, 1)
            };

            nestedTextBoxes.Add(new BasicTextBox
            {
                PlaceholderText = @"Nested textbox 1",
                Size = new Vector2(457, 30),
                TabbableContentContainer = otherTextBoxes
            });

            nestedTextBoxes.Add(new BasicTextBox
            {
                PlaceholderText = @"Nested textbox 2",
                Size = new Vector2(457, 30),
                TabbableContentContainer = otherTextBoxes
            });

            nestedTextBoxes.Add(new BasicTextBox
            {
                PlaceholderText = @"Nested textbox 3",
                Size = new Vector2(457, 30),
                TabbableContentContainer = otherTextBoxes
            });

            otherTextBoxes.Add(nestedTextBoxes);

            Add(otherTextBoxes);

            //textBoxes.Add(tb = new PasswordTextBox(@"", 14, Vector2.Zero, 300));
            AddStep(@"set number text", () => numbers.Text = @"1h2e3l4l5o6");
            AddAssert(@"number text only numbers", () => numbers.Text == @"123456");
        }

        private class NumberTextBox : BasicTextBox
        {
            protected override bool CanAddCharacter(char character) => char.IsNumber(character);
        }
    }
}
