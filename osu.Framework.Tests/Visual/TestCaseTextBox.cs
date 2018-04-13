// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Testing;
using OpenTK;

namespace osu.Framework.Tests.Visual
{
    public class TestCaseTextBox : TestCase
    {
        public TestCaseTextBox()
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

            textBoxes.Add(new TextBox
            {
                Size = new Vector2(100, 16),
                TabbableContentContainer = textBoxes
            });

            textBoxes.Add(new TextBox
            {
                Text = @"Limited length",
                Size = new Vector2(200, 20),
                LengthLimit = 20,
                TabbableContentContainer = textBoxes
            });

            textBoxes.Add(new TextBox
            {
                Text = @"Box with some more text",
                Size = new Vector2(500, 30),
                TabbableContentContainer = textBoxes
            });

            textBoxes.Add(new TextBox
            {
                PlaceholderText = @"Placeholder text",
                Size = new Vector2(500, 30),
                TabbableContentContainer = textBoxes
            });

            textBoxes.Add(new TextBox
            {
                Text = @"prefilled placeholder",
                PlaceholderText = @"Placeholder text",
                Size = new Vector2(500, 30),
                TabbableContentContainer = textBoxes
            });

            textBoxes.Add(new TextBox
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

            otherTextBoxes.Add(new TextBox
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

            nestedTextBoxes.Add(new TextBox
            {
                PlaceholderText = @"Nested textbox 1",
                Size = new Vector2(457, 30),
                TabbableContentContainer = otherTextBoxes
            });

            nestedTextBoxes.Add(new TextBox
            {
                PlaceholderText = @"Nested textbox 2",
                Size = new Vector2(457, 30),
                TabbableContentContainer = otherTextBoxes
            });

            nestedTextBoxes.Add(new TextBox
            {
                PlaceholderText = @"Nested textbox 3",
                Size = new Vector2(457, 30),
                TabbableContentContainer = otherTextBoxes
            });

            otherTextBoxes.Add(nestedTextBoxes);

            Add(otherTextBoxes);

            //textBoxes.Add(tb = new PasswordTextBox(@"", 14, Vector2.Zero, 300));
        }
    }
}
