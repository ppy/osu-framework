// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Testing;
using osuTK;
using osuTK.Input;

namespace osu.Framework.Tests.Visual.UserInterface
{
    public class TestSceneTextBox : ManualInputManagerTestScene
    {
        public override IReadOnlyList<Type> RequiredTypes => new[]
        {
            typeof(BasicTextBox),
            typeof(TextBox),
            typeof(PasswordTextBox)
        };

        private FillFlowContainer textBoxes;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            Schedule(() =>
            {
                Child = textBoxes = new FillFlowContainer
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

        [Test]
        public void VariousTextBoxes()
        {
            AddStep("add textboxes", () =>
            {
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
            });
        }

        [Test]
        public void TestNumbersOnly()
        {
            NumberTextBox numbers = null;

            AddStep("add number textbox", () =>
            {
                textBoxes.Add(numbers = new NumberTextBox
                {
                    PlaceholderText = @"Only numbers",
                    Size = new Vector2(500, 30),
                    TabbableContentContainer = textBoxes
                });
            });

            AddStep(@"set number text", () => numbers.Text = @"1h2e3l4l5o6");
            AddAssert(@"number text only numbers", () => numbers.Text == @"123456");
        }

        [TestCase(true, true)]
        [TestCase(true, false)]
        [TestCase(false, false)]
        public void CommitOnFocusLost(bool commitOnFocusLost, bool changeText)
        {
            InsertableTextBox textBox = null;

            bool wasNewText = false;
            int commitCount = 0;

            AddStep("add commit on unfocus textbox", () =>
            {
                wasNewText = false;
                commitCount = 0;

                textBoxes.Add(textBox = new InsertableTextBox
                {
                    Text = "Default Text",
                    CommitOnFocusLost = commitOnFocusLost,
                    Size = new Vector2(500, 30),
                    OnCommit = (_, newText) =>
                    {
                        commitCount++;
                        wasNewText = newText;
                    }
                });
            });

            AddAssert("ensure no commits", () => commitCount == 0);

            AddStep("click on textbox", () =>
            {
                InputManager.MoveMouseTo(textBox);
                InputManager.Click(MouseButton.Left);
            });

            if (changeText)
                AddStep("insert more text", () => textBox.InsertString(" Plus More"));

            AddStep("click away", () =>
            {
                InputManager.MoveMouseTo(Vector2.One);
                InputManager.Click(MouseButton.Left);
            });

            if (commitOnFocusLost)
            {
                AddAssert("ensure one commit", () => commitCount == 1);
                AddAssert("ensure new text", () => wasNewText == changeText);
            }
            else
                AddAssert("ensure no commits", () => commitCount == 0);

            AddStep("click on textbox", () =>
            {
                InputManager.MoveMouseTo(textBox);
                InputManager.Click(MouseButton.Left);
            });

            if (changeText)
                AddStep("insert more text", () => textBox.InsertString(" Plus More"));

            AddStep("commit via enter", () => InputManager.PressKey(Key.Enter));

            int expectedCount = 1 + (commitOnFocusLost ? 1 : 0);

            AddAssert($"ensure {expectedCount} commit(s)", () => commitCount == expectedCount);
            AddAssert("ensure new text", () => wasNewText == changeText);
        }

        private class InsertableTextBox : BasicTextBox
        {
            public new void InsertString(string text) => base.InsertString(text);
        }

        private class NumberTextBox : BasicTextBox
        {
            protected override bool CanAddCharacter(char character) => char.IsNumber(character);
        }
    }
}
