// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.UserInterface;
using OpenTK;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Screens.Testing;

namespace osu.Framework.VisualTests.Tests
{
    class TestCaseTextBox : TestCase
    {
        public override string Name => @"TextBox";

        public override string Description => @"Text entry evolved";

        public override void Reset()
        {
            base.Reset();

            FlowContainer textBoxes = new FlowContainer
            {
                Direction = FlowDirections.Vertical,
                Padding = new MarginPadding
                {
                    Top = 50,
                    Left = -50
                },
                Spacing = new Vector2(0, 50),
                Anchor = Anchor.TopCentre,
                Origin = Anchor.TopCentre,
                RelativeSizeAxes = Axes.Both,
                Size = new Vector2(0.8f, 1)
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

            FlowContainer otherTextBoxes = new FlowContainer
            {
                Direction = FlowDirections.Vertical,
                Padding = new MarginPadding
                {
                    Top = 50,
                    Left = 500
                },
                Spacing = new Vector2(0, 50),
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

            FlowContainer nestedTextBoxes = new FlowContainer
            {
                Direction = FlowDirections.Vertical,
                Spacing = new Vector2(0, 50),
                Anchor = Anchor.TopCentre,
                Origin = Anchor.TopCentre,
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
