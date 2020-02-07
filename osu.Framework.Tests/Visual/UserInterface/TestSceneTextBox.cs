// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input;
using osu.Framework.Testing;
using osuTK;
using osuTK.Graphics;
using osuTK.Input;

namespace osu.Framework.Tests.Visual.UserInterface
{
    public class TestSceneTextBox : ManualInputManagerTestScene
    {
        public override IReadOnlyList<Type> RequiredTypes => new[]
        {
            typeof(BasicTextBox),
            typeof(TextBox),
            typeof(BasicPasswordTextBox)
        };

        private FillFlowContainer textBoxes;

        [SetUp]
        public new void SetUp() => Schedule(() =>
        {
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
        });

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

                textBoxes.Add(new CustomTextBox
                {
                    Text = @"Custom textbox",
                    Size = new Vector2(500, 30),
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

                otherTextBoxes.Add(new BasicPasswordTextBox
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

            AddStep("commit via enter", () =>
            {
                InputManager.PressKey(Key.Enter);
                InputManager.ReleaseKey(Key.Enter);
            });

            int expectedCount = 1 + (commitOnFocusLost ? 1 : 0);

            AddAssert($"ensure {expectedCount} commit(s)", () => commitCount == expectedCount);
            AddAssert("ensure new text", () => wasNewText == changeText);
        }

        [Test]
        public void TestPreviousWordDeletion()
        {
            InsertableTextBox textBox = null;

            AddStep("add textbox", () =>
            {
                textBoxes.Add(textBox = new InsertableTextBox
                {
                    Size = new Vector2(200, 40),
                });
            });

            AddStep("click on textbox", () =>
            {
                InputManager.MoveMouseTo(textBox);
                InputManager.Click(MouseButton.Left);
            });

            AddStep("insert three words", () => textBox.InsertString("some long text"));
            AddStep("delete last word", () => textBox.DeletePreviousWord());
            AddAssert("two words remain", () => textBox.Text == "some long ");
            AddStep("delete last word", () => textBox.DeletePreviousWord());
            AddAssert("one word remains", () => textBox.Text == "some ");
            AddStep("delete last word", () => textBox.DeletePreviousWord());
            AddAssert("text is empty", () => textBox.Text.Length == 0);
            AddStep("delete last word", () => textBox.DeletePreviousWord());
            AddAssert("text is empty", () => textBox.Text.Length == 0);
        }

        [Test]
        public void TestNextWordDeletion()
        {
            InsertableTextBox textBox = null;

            AddStep("add textbox", () =>
            {
                textBoxes.Add(textBox = new InsertableTextBox
                {
                    Size = new Vector2(200, 40)
                });
            });

            AddStep("click on textbox", () =>
            {
                InputManager.MoveMouseTo(textBox);
                InputManager.Click(MouseButton.Left);
            });

            AddStep("insert three words", () => textBox.InsertString("some long text"));
            AddStep("move caret to start", () => textBox.MoveToStart());
            AddStep("delete first word", () => textBox.DeleteNextWord());
            AddAssert("two words remain", () => textBox.Text == " long text");
            AddStep("delete first word", () => textBox.DeleteNextWord());
            AddAssert("one word remains", () => textBox.Text == " text");
            AddStep("delete first word", () => textBox.DeleteNextWord());
            AddAssert("text is empty", () => textBox.Text.Length == 0);
            AddStep("delete first word", () => textBox.DeleteNextWord());
            AddAssert("text is empty", () => textBox.Text.Length == 0);
        }

        /// <summary>
        /// Removes first 2 characters and append them, this tests layout positioning of the characters in the text box.
        /// </summary>
        [Test]
        public void TestRemoveAndAppend()
        {
            InsertableTextBox textBox = null;

            AddStep("add textbox", () =>
            {
                textBoxes.Add(textBox = new InsertableTextBox
                {
                    Size = new Vector2(200, 40),
                });
            });

            AddStep("focus textbox", () =>
            {
                InputManager.MoveMouseTo(textBox);
                InputManager.Click(MouseButton.Left);
            });

            AddStep("insert word", () => textBox.InsertString("eventext"));
            AddStep("remove 2 letters", () => textBox.RemoveFirstCharacters(2));
            AddStep("append string", () => textBox.AppendString("ev"));
            AddStep("remove 2 letters", () => textBox.RemoveFirstCharacters(2));
            AddStep("append string", () => textBox.AppendString("en"));
            AddStep("remove 2 letters", () => textBox.RemoveFirstCharacters(2));
            AddStep("append string", () => textBox.AppendString("te"));
            AddStep("remove 2 letters", () => textBox.RemoveFirstCharacters(2));
            AddStep("append string", () => textBox.AppendString("xt"));
            AddAssert("is correct displayed text", () => textBox.FlowingText == "eventext" && textBox.FlowingText == textBox.Text);
        }

        /// <summary>
        /// Removes last 2 characters and prepend them, this tests layout positioning of the characters in the text box.
        /// </summary>
        [Test]
        public void TestRemoveAndPrepend()
        {
            InsertableTextBox textBox = null;

            AddStep("add textbox", () =>
            {
                textBoxes.Add(textBox = new InsertableTextBox
                {
                    Size = new Vector2(200, 40),
                });
            });

            AddStep("focus textbox", () =>
            {
                InputManager.MoveMouseTo(textBox);
                InputManager.Click(MouseButton.Left);
            });

            AddStep("insert word", () => textBox.InsertString("eventext"));
            AddStep("remove 2 letters", () => textBox.RemoveLastCharacters(2));
            AddStep("prepend string", () => textBox.PrependString("xt"));
            AddStep("remove 2 letters", () => textBox.RemoveLastCharacters(2));
            AddStep("prepend string", () => textBox.PrependString("te"));
            AddStep("remove 2 letters", () => textBox.RemoveLastCharacters(2));
            AddStep("prepend string", () => textBox.PrependString("en"));
            AddStep("remove 2 letters", () => textBox.RemoveLastCharacters(2));
            AddStep("prepend string", () => textBox.PrependString("ev"));
            AddAssert("is correct displayed text", () => textBox.FlowingText == "eventext" && textBox.FlowingText == textBox.Text);
        }

        private class InsertableTextBox : BasicTextBox
        {
            /// <summary>
            /// Returns the shown-in-screen text.
            /// </summary>
            public string FlowingText => string.Concat(TextFlow.FlowingChildren.OfType<FallingDownContainer>().Select(c => c.OfType<SpriteText>().Single().Text.ToString()[0]));

            public new void InsertString(string text) => base.InsertString(text);

            public void PrependString(string text)
            {
                MoveToStart();
                InsertString(text);
            }

            public void AppendString(string text)
            {
                MoveToEnd();
                InsertString(text);
            }

            public void RemoveFirstCharacters(int count)
            {
                MoveToStart();

                for (int i = 0; i < count; i++)
                    DeleteNextCharacter();
            }

            public void RemoveLastCharacters(int count)
            {
                MoveToEnd();

                for (int i = 0; i < count; i++)
                    DeletePreviousCharacter();
            }

            public void MoveToStart() => OnPressed(new PlatformAction(PlatformActionType.LineStart, PlatformActionMethod.Move));
            public void MoveToEnd() => OnPressed(new PlatformAction(PlatformActionType.LineEnd, PlatformActionMethod.Move));

            public void DeletePreviousCharacter() => OnPressed(new PlatformAction(PlatformActionType.CharPrevious, PlatformActionMethod.Delete));
            public void DeleteNextCharacter() => OnPressed(new PlatformAction(PlatformActionType.CharNext, PlatformActionMethod.Delete));

            public void DeletePreviousWord() => OnPressed(new PlatformAction(PlatformActionType.WordPrevious, PlatformActionMethod.Delete));
            public void DeleteNextWord() => OnPressed(new PlatformAction(PlatformActionType.WordNext, PlatformActionMethod.Delete));
        }

        private class NumberTextBox : BasicTextBox
        {
            protected override bool CanAddCharacter(char character) => char.IsNumber(character);
        }

        private class CustomTextBox : BasicTextBox
        {
            protected override Drawable GetDrawableCharacter(char c) => new ScalingText(c, CalculatedTextSize);

            private class ScalingText : CompositeDrawable
            {
                private readonly SpriteText text;

                public ScalingText(char c, float textSize)
                {
                    AddInternal(text = new SpriteText
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Text = c.ToString(),
                        Font = FrameworkFont.Condensed.With(size: textSize),
                    });
                }

                protected override void LoadComplete()
                {
                    base.LoadComplete();

                    Size = text.DrawSize;
                }

                public override void Show()
                {
                    text.Scale = Vector2.Zero;
                    text.FadeIn(200).ScaleTo(1, 200);
                }

                public override void Hide()
                {
                    text.Scale = Vector2.One;
                    text.ScaleTo(0, 200).FadeOut(200);
                }
            }

            protected override Caret CreateCaret() => new BorderCaret();

            private class BorderCaret : Caret
            {
                private const float caret_width = 2;

                public BorderCaret()
                {
                    RelativeSizeAxes = Axes.Y;

                    Masking = true;
                    BorderColour = Color4.White;
                    BorderThickness = 3;

                    InternalChild = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.Transparent
                    };
                }

                public override void DisplayAt(Vector2 position, float? selectionWidth)
                {
                    Position = position - Vector2.UnitX;
                    Width = selectionWidth + 1 ?? caret_width;
                }
            }
        }
    }
}
