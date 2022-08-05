// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Linq;
using NUnit.Framework;
using osu.Framework.Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input;
using osu.Framework.Testing;
using osu.Framework.Utils;
using osuTK;
using osuTK.Graphics;
using osuTK.Input;

namespace osu.Framework.Tests.Visual.UserInterface
{
    public class TestSceneTextBox : ManualInputManagerTestScene
    {
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

            // <c>U+FF11</c> is the Unicode FULLWIDTH DIGIT ONE character, treated as a number by char.IsNumber()
            AddStep(@"set number text", () => numbers.Text = "1h2e3l4l5o6\uFF11");
            AddAssert(@"number text only numbers", () => numbers.Text == @"123456");
        }

        [TestCase(true, true, false)]
        [TestCase(true, false, false)]
        [TestCase(false, false, false)]
        [TestCase(true, true, true)]
        [TestCase(true, false, true)]
        [TestCase(false, false, true)]
        public void CommitOnFocusLost(bool commitOnFocusLost, bool changeText, bool withInitialText)
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
                    CommitOnFocusLost = commitOnFocusLost,
                    Size = new Vector2(500, 30),
                });

                if (withInitialText)
                    textBox.Text = "Default Text";

                textBox.OnCommit += (_, newText) =>
                {
                    commitCount++;
                    wasNewText = newText;
                };
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
        public void TestBackspaceWhileShifted()
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

            AddStep("type character", () =>
            {
                // tests don't actually send consumable text, but this important part is that we fire the key event to begin consuming.
                InputManager.Key(Key.A);
                textBox.Text += "a";
            });

            AddStep("backspace character", () => InputManager.Key(Key.BackSpace));
            AddAssert("character removed", () => textBox.Text == string.Empty);

            AddStep("shift down", () => InputManager.PressKey(Key.ShiftLeft));

            AddStep("type character", () =>
            {
                InputManager.Key(Key.A);
                textBox.Text += "A";
            });

            AddStep("backspace character", () => InputManager.Key(Key.BackSpace));
            AddAssert("character removed", () => textBox.Text == string.Empty);

            AddStep("shift up", () => InputManager.ReleaseKey(Key.ShiftLeft));
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
            AddStep("delete last word", () => InputManager.Keys(PlatformAction.DeleteBackwardWord));
            AddAssert("two words remain", () => textBox.Text == "some long ");
            AddStep("delete last word", () => InputManager.Keys(PlatformAction.DeleteBackwardWord));
            AddAssert("one word remains", () => textBox.Text == "some ");
            AddStep("delete last word", () => InputManager.Keys(PlatformAction.DeleteBackwardWord));
            AddAssert("text is empty", () => textBox.Text.Length == 0);
            AddStep("delete last word", () => InputManager.Keys(PlatformAction.DeleteBackwardWord));
            AddAssert("text is empty", () => textBox.Text.Length == 0);
        }

        [Test]
        public void TestPreviousWordDeletionWithShortWords()
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

            AddStep("insert three words", () => textBox.InsertString("a b c"));
            AddStep("delete last word", () => InputManager.Keys(PlatformAction.DeleteBackwardWord));
            AddAssert("two words remain", () => textBox.Text == "a b ");
            AddStep("delete last word", () => InputManager.Keys(PlatformAction.DeleteBackwardWord));
            AddAssert("one word remains", () => textBox.Text == "a ");
            AddStep("delete last word", () => InputManager.Keys(PlatformAction.DeleteBackwardWord));
            AddAssert("text is empty", () => textBox.Text.Length == 0);
            AddStep("delete last word", () => InputManager.Keys(PlatformAction.DeleteBackwardWord));
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
            AddStep("move caret to start", () => InputManager.Keys(PlatformAction.MoveBackwardLine));
            AddStep("delete first word", () => InputManager.Keys(PlatformAction.DeleteForwardWord));
            AddAssert("two words remain", () => textBox.Text == " long text");
            AddStep("delete first word", () => InputManager.Keys(PlatformAction.DeleteForwardWord));
            AddAssert("one word remains", () => textBox.Text == " text");
            AddStep("delete first word", () => InputManager.Keys(PlatformAction.DeleteForwardWord));
            AddAssert("text is empty", () => textBox.Text.Length == 0);
            AddStep("delete first word", () => InputManager.Keys(PlatformAction.DeleteForwardWord));
            AddAssert("text is empty", () => textBox.Text.Length == 0);
        }

        [Test]
        public void TestNextWordDeletionWithShortWords()
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

            AddStep("insert three words", () => textBox.InsertString("a b c"));
            AddStep("move caret to start", () => InputManager.Keys(PlatformAction.MoveBackwardLine));
            AddStep("delete first word", () => InputManager.Keys(PlatformAction.DeleteForwardWord));
            AddAssert("two words remain", () => textBox.Text == " b c");
            AddStep("delete first word", () => InputManager.Keys(PlatformAction.DeleteForwardWord));
            AddAssert("one word remains", () => textBox.Text == " c");
            AddStep("delete first word", () => InputManager.Keys(PlatformAction.DeleteForwardWord));
            AddAssert("text is empty", () => textBox.Text.Length == 0);
            AddStep("delete first word", () => InputManager.Keys(PlatformAction.DeleteForwardWord));
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
            AddStep("remove 2 letters", () => removeFirstCharacters(2));
            AddStep("append string", () => appendString(textBox, "ev"));
            AddStep("remove 2 letters", () => removeFirstCharacters(2));
            AddStep("append string", () => appendString(textBox, "en"));
            AddStep("remove 2 letters", () => removeFirstCharacters(2));
            AddStep("append string", () => appendString(textBox, "te"));
            AddStep("remove 2 letters", () => removeFirstCharacters(2));
            AddStep("append string", () => appendString(textBox, "xt"));
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
            AddStep("remove 2 letters", () => removeLastCharacters(2));
            AddStep("prepend string", () => prependString(textBox, "xt"));
            AddStep("remove 2 letters", () => removeLastCharacters(2));
            AddStep("prepend string", () => prependString(textBox, "te"));
            AddStep("remove 2 letters", () => removeLastCharacters(2));
            AddStep("prepend string", () => prependString(textBox, "en"));
            AddStep("remove 2 letters", () => removeLastCharacters(2));
            AddStep("prepend string", () => prependString(textBox, "ev"));
            AddAssert("is correct displayed text", () => textBox.FlowingText == "eventext" && textBox.FlowingText == textBox.Text);
        }

        [Test]
        public void TestReplaceSelectionWhileLimited()
        {
            InsertableTextBox textBox = null;

            AddStep("add limited textbox", () =>
            {
                textBoxes.Add(textBox = new InsertableTextBox
                {
                    Size = new Vector2(200, 40),
                    Text = "some text",
                });

                textBox.LengthLimit = textBox.Text.Length;
            });

            AddStep("focus textbox", () =>
            {
                InputManager.MoveMouseTo(textBox);
                InputManager.Click(MouseButton.Left);
            });

            AddStep("select all", () => InputManager.Keys(PlatformAction.SelectAll));
            AddStep("insert string", () => textBox.InsertString("another"));
            AddAssert("text replaced", () => textBox.FlowingText == "another" && textBox.FlowingText == textBox.Text);
        }

        [Test]
        public void TestReadOnly()
        {
            BasicTextBox firstTextBox = null;
            BasicTextBox secondTextBox = null;

            AddStep("add textboxes", () => textBoxes.AddRange(new[]
            {
                firstTextBox = new BasicTextBox
                {
                    Text = "Readonly textbox",
                    Size = new Vector2(500, 30),
                    ReadOnly = true,
                    TabbableContentContainer = textBoxes
                },
                secondTextBox = new BasicTextBox
                {
                    Text = "Standard textbox",
                    Size = new Vector2(500, 30),
                    TabbableContentContainer = textBoxes
                }
            }));

            AddStep("click first (readonly) textbox", () =>
            {
                InputManager.MoveMouseTo(firstTextBox);
                InputManager.Click(MouseButton.Left);
            });
            AddAssert("first textbox has no focus", () => !firstTextBox.HasFocus);

            AddStep("click second (editable) textbox", () =>
            {
                InputManager.MoveMouseTo(secondTextBox);
                InputManager.Click(MouseButton.Left);
            });
            AddStep("try to tab backwards", () =>
            {
                InputManager.PressKey(Key.ShiftLeft);
                InputManager.Key(Key.Tab);
                InputManager.ReleaseKey(Key.ShiftLeft);
            });
            AddAssert("first (readonly) has no focus", () => !firstTextBox.HasFocus);

            AddStep("drag on first (readonly) textbox", () =>
            {
                InputManager.MoveMouseTo(firstTextBox.ScreenSpaceDrawQuad.Centre);
                InputManager.PressButton(MouseButton.Left);
                InputManager.MoveMouseTo(firstTextBox.ScreenSpaceDrawQuad.TopLeft);
                InputManager.ReleaseButton(MouseButton.Left);
            });
            AddAssert("first textbox has no focus", () => !firstTextBox.HasFocus);

            AddStep("make first textbox non-readonly", () => firstTextBox.ReadOnly = false);
            AddStep("click first textbox", () =>
            {
                InputManager.MoveMouseTo(firstTextBox);
                InputManager.Click(MouseButton.Left);
            });
            AddStep("make first textbox readonly again", () => firstTextBox.ReadOnly = true);
            AddAssert("first textbox yielded focus", () => !firstTextBox.HasFocus);
            AddStep("delete last character", () => InputManager.Keys(PlatformAction.DeleteBackwardChar));
            AddAssert("no text removed", () => firstTextBox.Text == "Readonly textbox");
        }

        [Test]
        public void TestValueCorrectionViaCurrent()
        {
            InsertableTextBox textBox = null;

            AddStep("add textbox", () => textBoxes.AddRange(new[]
            {
                textBox = new InsertableTextBox
                {
                    Text = "24",
                    Size = new Vector2(500, 30),
                    TabbableContentContainer = textBoxes
                },
            }));

            AddStep("register current callback", () => textBox.Current.BindValueChanged(text =>
            {
                if (string.IsNullOrEmpty(text.NewValue))
                    return;

                if (!int.TryParse(text.NewValue, out int value) || value > 100)
                    textBox.Current.Value = "0";
            }));

            AddStep("click textbox", () =>
            {
                InputManager.MoveMouseTo(textBox);
                InputManager.Click(MouseButton.Left);
            });
            AddStep("insert digit", () => textBox.InsertString("9"));
            AddUntilStep("textbox value is 0", () => textBox.Current.Value == "0");
            AddUntilStep("caret is in correct position", () =>
            {
                var spriteText = textBox.ChildrenOfType<SpriteText>().SingleOrDefault(text => text.Text == "0");
                var caret = textBox.ChildrenOfType<Caret>().Single();

                return spriteText != null && Precision.AlmostEquals(
                    spriteText.ScreenSpaceDrawQuad.TopRight.X,
                    caret.ScreenSpaceDrawQuad.TopLeft.X,
                    5f);
            });
        }

        [Test]
        public void TestInputOverride()
        {
            InsertableTextBox overrideInputBox = null;

            AddStep("add override textbox", () =>
            {
                textBoxes.Add(overrideInputBox = new InsertableTextBox
                {
                    Text = @"Override input textbox",
                    Size = new Vector2(500, 30),
                    TabbableContentContainer = textBoxes
                });
                overrideInputBox.Current.BindValueChanged(vce =>
                {
                    if (vce.NewValue != @"Input overridden!")
                        overrideInputBox.Current.Value = @"Input overridden!";
                });
            });

            AddStep(@"set some text", () => overrideInputBox.Text = "smth");
            AddAssert(@"verify display state", () => overrideInputBox.FlowingText == "Input overridden!");
        }

        [Test]
        public void TestDisableAndSetText()
        {
            InsertableTextBox textBox = null;

            AddStep("add text box", () =>
            {
                textBoxes.Add(textBox = new InsertableTextBox
                {
                    Size = new Vector2(200, 40),
                    Text = "hello"
                });
            });
            AddAssert("text is hello", () => textBox.Text == "hello");

            AddStep("set new text and disable", () =>
            {
                textBox.Text = "goodbye";
                textBox.Current.Disabled = true;
            });
            AddAssert("text is goodbye", () => textBox.Text == "goodbye");

            AddStep("attempt to set text", () => textBox.Text = "change!");
            AddAssert("text is unchanged", () => textBox.Text == "goodbye");

            AddStep("attempt to insert text", () => textBox.InsertString("maybe this way?"));
            AddAssert("text is unchanged", () => textBox.Text == "goodbye");
        }

        [Test]
        public void TestLongTextMovesTextContainer()
        {
            PaddedTextBox textBox = null;

            AddStep("add textbox", () =>
            {
                textBoxes.Add(textBox = new PaddedTextBox
                {
                    Size = new Vector2(300, 40),
                    Text = "hello",
                });
            });

            AddAssert("text container didn't move", () => Precision.AlmostEquals(textBox.TextContainerBounds.TopLeft.X, PaddedTextBox.LEFT_RIGHT_PADDING, 1));

            AddStep("set long text", () => textBox.Text = "this is very long text in a box");
            AddUntilStep("wait for transforms to finish", () => textBox.TextContainerTransformsFinished);
            AddAssert("text container moved to expected position", () => Precision.AlmostEquals(textBox.TextContainerBounds.TopRight.X, textBox.DrawWidth - PaddedTextBox.LEFT_RIGHT_PADDING, 1));
        }

        [Test]
        public void TestMovingCaretMovesTextContainer()
        {
            PaddedTextBox textBox = null;

            AddStep("add textbox", () =>
            {
                textBoxes.Add(textBox = new PaddedTextBox
                {
                    Size = new Vector2(300, 40),
                    Text = "framework framework framework framework framework framework framework"
                });
            });

            AddStep("click on textbox", () =>
            {
                InputManager.MoveMouseTo(textBox);
                InputManager.Click(MouseButton.Left);
            });
            AddStep("move caret to start", () => InputManager.Keys(PlatformAction.MoveBackwardLine));
            AddUntilStep("wait for transforms to finish", () => textBox.TextContainerTransformsFinished);
            AddAssert("text container moved to start", () => Precision.AlmostEquals(textBox.TextContainerBounds.TopLeft.X, PaddedTextBox.LEFT_RIGHT_PADDING, 1));

            AddStep("move forward word", () => InputManager.Keys(PlatformAction.MoveForwardWord));
            AddUntilStep("wait for transforms to finish", () => textBox.TextContainerTransformsFinished);
            AddAssert("text container didn't move", () => Precision.AlmostEquals(textBox.TextContainerBounds.TopLeft.X, PaddedTextBox.LEFT_RIGHT_PADDING, 1));

            AddStep("move forward word", () => InputManager.Keys(PlatformAction.MoveForwardWord));
            AddUntilStep("wait for transforms to finish", () => textBox.TextContainerTransformsFinished);
            AddAssert("text container moved back", () => textBox.TextContainerBounds.TopLeft.X < PaddedTextBox.LEFT_RIGHT_PADDING);
        }

        [Test]
        public void TestClickingToMoveCaretMovesTextContainer()
        {
            PaddedTextBox textBox = null;

            AddStep("add textbox", () =>
            {
                textBoxes.Add(textBox = new PaddedTextBox
                {
                    Size = new Vector2(300, 40),
                    Text = "this is very long text in a box that will scroll",
                });
            });

            AddStep("click on textbox", () =>
            {
                InputManager.MoveMouseTo(textBox);
                InputManager.Click(MouseButton.Left);
            });
            AddStep("move caret to start", () => InputManager.Keys(PlatformAction.MoveBackwardLine));
            AddUntilStep("wait for transforms to finish", () => textBox.TextContainerTransformsFinished);
            AddAssert("text container moved to start", () => Precision.AlmostEquals(textBox.TextContainerBounds.TopLeft.X, PaddedTextBox.LEFT_RIGHT_PADDING, 1));

            AddStep("click close to the right edge of textbox", () =>
            {
                InputManager.MoveMouseTo((textBox.ScreenSpaceDrawQuad.TopRight + textBox.ScreenSpaceDrawQuad.BottomRight) / 2 - new Vector2(1, 0));
                InputManager.Click(MouseButton.Left);
            });
            AddUntilStep("wait for transforms to finish", () => textBox.TextContainerTransformsFinished);
            AddAssert("text container moved back", () => textBox.TextContainerBounds.TopLeft.X < PaddedTextBox.LEFT_RIGHT_PADDING);
        }

        [Test]
        public void TestSetTextSelection()
        {
            TextBox textBox = null;

            AddStep("add textbox", () =>
            {
                textBoxes.Add(textBox = new BasicTextBox
                {
                    Size = new Vector2(300, 40),
                    Text = "initial text",
                });
            });

            AddStep("click on textbox", () =>
            {
                InputManager.MoveMouseTo(textBox);
                InputManager.Click(MouseButton.Left);
            });

            AddStep("set text", () => textBox.Text = "a longer string of text");
            // ideally, this should check the caret/selection position, but that is not exposed in TextBox.
            AddAssert("nothing selected", () => textBox.SelectedText == string.Empty);

            AddStep("select all", () => InputManager.Keys(PlatformAction.SelectAll));
            AddStep("set text via current", () => textBox.Text = "short text");
            AddAssert("nothing selected", () => textBox.SelectedText == string.Empty);
        }

        private void prependString(InsertableTextBox textBox, string text)
        {
            InputManager.Keys(PlatformAction.MoveBackwardLine);

            ScheduleAfterChildren(() => textBox.InsertString(text));
        }

        private void appendString(InsertableTextBox textBox, string text)
        {
            InputManager.Keys(PlatformAction.MoveForwardLine);

            ScheduleAfterChildren(() => textBox.InsertString(text));
        }

        private void removeFirstCharacters(int count)
        {
            InputManager.Keys(PlatformAction.MoveBackwardLine);

            for (int i = 0; i < count; i++)
                InputManager.Keys(PlatformAction.DeleteForwardChar);
        }

        private void removeLastCharacters(int count)
        {
            InputManager.Keys(PlatformAction.MoveForwardLine);

            for (int i = 0; i < count; i++)
                InputManager.Keys(PlatformAction.DeleteBackwardChar);
        }

        public class InsertableTextBox : BasicTextBox
        {
            /// <summary>
            /// Returns the shown-in-screen text.
            /// </summary>
            public string FlowingText => string.Concat(TextFlow.FlowingChildren.OfType<FallingDownContainer>().Select(c => c.OfType<SpriteText>().Single().Text.ToString()[0]));

            public new void InsertString(string text) => base.InsertString(text);
        }

        private class NumberTextBox : BasicTextBox
        {
            protected override bool CanAddCharacter(char character) => character.IsAsciiDigit();

            protected override bool AllowIme => false;
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

        private class PaddedTextBox : BasicTextBox
        {
            public const float LEFT_RIGHT_PADDING = 50;

            protected override float LeftRightPadding => LEFT_RIGHT_PADDING;

            public Quad TextContainerBounds => TextContainer.ToSpaceOfOtherDrawable(new RectangleF(Vector2.Zero, TextContainer.DrawSize), this);

            public bool TextContainerTransformsFinished => TextContainer.LatestTransformEndTime == TextContainer.TransformStartTime;
        }
    }
}
