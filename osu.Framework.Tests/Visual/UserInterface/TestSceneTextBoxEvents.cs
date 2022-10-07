// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input;
using osu.Framework.Testing;
using osuTK;
using osuTK.Input;

namespace osu.Framework.Tests.Visual.UserInterface
{
    public class TestSceneTextBoxEvents : ManualInputManagerTestScene
    {
        private EventQueuesTextBox textBox;
        private ManualTextInput textInput;
        private ManualTextInputContainer textInputContainer;

        private const string default_text = "some default text";
        private const string composition_text = "test";

        [SetUpSteps]
        public void SetUpSteps()
        {
            AddStep("add manual text input container", () =>
            {
                Child = textInputContainer = new ManualTextInputContainer();
                textInput = textInputContainer.TextInput;
            });

            AddStep("add textbox", () => textInputContainer.Add(textBox = new EventQueuesTextBox
            {
                CommitOnFocusLost = true,
                ReleaseFocusOnCommit = false,
                Size = new Vector2(200, 40),
                Text = default_text,
            }));

            AddStep("focus textbox", () =>
            {
                InputManager.MoveMouseTo(textBox);
                InputManager.Click(MouseButton.Left);
            });
            AddStep("dequeue text input activated event", () => textInput.ActivationQueue.Dequeue());

            AddStep("move caret to end", () => InputManager.Keys(PlatformAction.MoveForwardLine));
            AddStep("dequeue caret event", () => textBox.CaretMovedQueue.Dequeue());
        }

        [Test]
        public void TestCommitIsNewTextSecondTime()
        {
            AddStep("add handler to reset on commit", () => textBox.OnCommit += (_, isNew) =>
            {
                if (!isNew)
                    return;

                textBox.Text = default_text;
            });

            AddStep("insert text", () => textBox.InsertString("temporary text"));
            AddStep("press enter key for committing text", () => InputManager.Key(Key.Enter));
            AddAssert("text committed event raised with new", () => textBox.CommittedTextQueue.Dequeue());
            AddAssert("text is restored to default by event", () => textBox.Text == default_text);

            AddStep("insert text", () => textBox.InsertString("temporary text"));
            AddStep("press enter key for committing text", () => InputManager.Key(Key.Enter));
            AddAssert("text committed event raised with new", () => textBox.CommittedTextQueue.Dequeue());
            AddAssert("text is restored to default by event", () => textBox.Text == default_text);
        }

        [Test]
        public void TestMutatingTextPropertyDoesntInvokeEvent()
        {
            AddStep("set text property once more", () => textBox.Text = "set property once more");
            AddStep("insert string via protected method", () => textBox.InsertString(" inserted string."));
            AddAssert("no user text consumed event", () => textBox.UserConsumedTextQueue.Count == 0);
        }

        [Test]
        public void TestInsertingUserTextInvokesEvent()
        {
            AddStep("press letter key to insert text", () =>
            {
                // TextBox expects text input to arrive before the associated key press.
                textInput.Text("W");
                InputManager.Key(Key.W);
            });
            AddAssert("user text consumed event", () => textBox.UserConsumedTextQueue.Dequeue() == "W" && textBox.UserConsumedTextQueue.Count == 0);
        }

        [Test]
        public void TestDeletingPrevCharActionInvokesEvent()
        {
            string lastText = null;

            AddStep("invoke delete action to remove text", () =>
            {
                lastText = textBox.Text;
                InputManager.Keys(PlatformAction.DeleteBackwardChar);
            });
            AddAssert("user text removed event raised", () => textBox.UserRemovedTextQueue.Dequeue() == lastText.Last().ToString() && textBox.UserRemovedTextQueue.Count == 0);
        }

        [Test]
        public void TestCommittingTextInvokesEvents()
        {
            AddStep("insert text", () => textBox.InsertString(" with another"));
            AddStep("press enter key for committing text", () => InputManager.Key(Key.Enter));

            AddAssert("text committed event raised", () =>
                // Ensure dequeued text commit event has textChanged = true.
                textBox.CommittedTextQueue.Dequeue() && textBox.CommittedTextQueue.Count == 0);

            AddStep("click away", () =>
            {
                InputManager.MoveMouseTo(textBox.ScreenSpaceDrawQuad.BottomRight + Vector2.One * 20);
                InputManager.Click(MouseButton.Left);
            });

            AddAssert("text committed event raised", () =>
                // Ensure dequeued text commit event has textChanged = false.
                textBox.CommittedTextQueue.Dequeue() == false && textBox.CommittedTextQueue.Count == 0);

            AddAssert("input deactivated", () => textInput.DeactivationQueue.Dequeue() && textInput.DeactivationQueue.Count == 0);
        }

        [Test]
        public void TestMovingOrExpandingSelectionInvokesEvent()
        {
            // Selecting Forward
            AddStep("invoke move action to move caret", () => InputManager.Keys(PlatformAction.MoveBackwardLine));
            AddAssert("caret moved event", () =>
                // Ensure dequeued caret move event has selecting = false.
                textBox.CaretMovedQueue.Dequeue() == false && textBox.CommittedTextQueue.Count == 0);

            AddStep("invoke select action to expand selection", () => InputManager.Keys(PlatformAction.SelectForwardChar));
            AddAssert("text selection event (character)", () => textBox.TextSelectionQueue.Dequeue() == TextBox.TextSelectionType.Character);
            AddAssert("caret moved event", () =>
                // Ensure dequeued caret move event has selecting = true.
                textBox.CaretMovedQueue.Dequeue() && textBox.CommittedTextQueue.Count == 0);

            AddStep("invoke move action to move caret", () => InputManager.Keys(PlatformAction.MoveBackwardLine));
            AddAssert("text deselect event", () => textBox.TextDeselectionQueue.Dequeue());
            AddAssert("caret moved event", () =>
                // Ensure dequeued caret move event has selecting = false.
                textBox.CaretMovedQueue.Dequeue() == false && textBox.CommittedTextQueue.Count == 0);

            AddStep("invoke select action to expand selection", () => InputManager.Keys(PlatformAction.SelectForwardWord));
            AddAssert("text selection event (word)", () => textBox.TextSelectionQueue.Dequeue() == TextBox.TextSelectionType.Word);
            AddAssert("caret moved event", () =>
                // Ensure dequeued caret move event has selecting = true.
                textBox.CaretMovedQueue.Dequeue() && textBox.CommittedTextQueue.Count == 0);

            // Selecting Backward
            AddStep("invoke move action to move caret", () => InputManager.Keys(PlatformAction.MoveForwardLine));
            AddAssert("text deselect event", () => textBox.TextDeselectionQueue.Dequeue());
            AddAssert("caret moved event", () =>
                // Ensure dequeued caret move event has selecting = false.
                textBox.CaretMovedQueue.Dequeue() == false && textBox.CommittedTextQueue.Count == 0);

            AddStep("invoke select action to expand selection", () => InputManager.Keys(PlatformAction.SelectBackwardChar));
            AddAssert("text selection event (character)", () => textBox.TextSelectionQueue.Dequeue() == TextBox.TextSelectionType.Character);
            AddAssert("caret moved event", () =>
                // Ensure dequeued caret move event has selecting = true.
                textBox.CaretMovedQueue.Dequeue() && textBox.CommittedTextQueue.Count == 0);

            AddStep("invoke move action to move caret", () => InputManager.Keys(PlatformAction.MoveForwardLine));
            AddAssert("text deselect event", () => textBox.TextDeselectionQueue.Dequeue());
            AddAssert("caret moved event", () =>
                // Ensure dequeued caret move event has selecting = false.
                textBox.CaretMovedQueue.Dequeue() == false && textBox.CommittedTextQueue.Count == 0);

            AddStep("invoke select action to expand selection", () => InputManager.Keys(PlatformAction.SelectBackwardWord));
            AddAssert("text selection event (word)", () => textBox.TextSelectionQueue.Dequeue() == TextBox.TextSelectionType.Word);
            AddAssert("caret moved event", () =>
                // Ensure dequeued caret move event has selecting = true.
                textBox.CaretMovedQueue.Dequeue() && textBox.CommittedTextQueue.Count == 0);

            // Selecting All
            AddStep("invoke select action to expand selection", () => InputManager.Keys(PlatformAction.SelectAll));
            AddAssert("text selection event (all)", () => textBox.TextSelectionQueue.Dequeue() == TextBox.TextSelectionType.All);

            AddStep("invoke move action to move caret", () => InputManager.Keys(PlatformAction.MoveBackwardLine));
            AddAssert("text deselect event", () => textBox.TextDeselectionQueue.Dequeue());
            AddAssert("caret moved event", () =>
                // Ensure dequeued caret move event has selecting = false.
                textBox.CaretMovedQueue.Dequeue() == false && textBox.CommittedTextQueue.Count == 0);

            // Selecting via Mouse
            AddStep("double-click selection", () =>
            {
                InputManager.MoveMouseTo(textBox);
                InputManager.Click(MouseButton.Left);
                InputManager.Click(MouseButton.Left);
            });
            AddAssert("text selection event (word)", () => textBox.TextSelectionQueue.Dequeue() == TextBox.TextSelectionType.Word);

            AddAssert("text input not deactivated", () => textInput.DeactivationQueue.Count == 0);
            AddAssert("text input not activated again", () => textInput.ActivationQueue.Count == 0);
            AddAssert("text input ensure activated", () => textInput.EnsureActivatedQueue.Dequeue() && textInput.EnsureActivatedQueue.Count == 0);

            AddStep("click deselection", () =>
            {
                InputManager.MoveMouseTo(textBox);
                InputManager.Click(MouseButton.Left);
            });
            AddAssert("text deselect event", () => textBox.TextDeselectionQueue.Dequeue());

            AddAssert("text input not deactivated", () => textInput.DeactivationQueue.Count == 0);
            AddAssert("text input not activated again", () => textInput.ActivationQueue.Count == 0);
            AddAssert("text input ensure activated", () => textInput.EnsureActivatedQueue.Dequeue() && textInput.EnsureActivatedQueue.Count == 0);

            AddStep("click-drag selection", () =>
            {
                InputManager.MoveMouseTo(textBox);
                InputManager.PressButton(MouseButton.Left);
                InputManager.MoveMouseTo(textInputContainer.ToScreenSpace(textBox.DrawRectangle.Centre + new Vector2(50, 0)));
                InputManager.ReleaseButton(MouseButton.Left);
            });
            AddAssert("text selection event (character)", () => textBox.TextSelectionQueue.Dequeue() == TextBox.TextSelectionType.Character);
        }

        [Test]
        public void TestSelectAfterOutOfBandSelectionChange()
        {
            AddStep("select all text", () => InputManager.Keys(PlatformAction.SelectAll));
            AddAssert("text selection event (all)", () => textBox.TextSelectionQueue.Dequeue() == TextBox.TextSelectionType.All);

            AddStep("delete all text", () => InputManager.Keys(PlatformAction.Delete));
            AddAssert("user text removed event raised", () => textBox.UserRemovedTextQueue.Dequeue() == default_text);

            AddAssert("no text is selected", () => textBox.SelectedText, () => Is.Empty);
            AddStep("invoke caret select action", () => InputManager.Keys(PlatformAction.SelectForwardChar));
            AddAssert("no text is selected", () => textBox.SelectedText, () => Is.Empty);

            AddAssert("no text selection event", () => textBox.TextSelectionQueue, () => Has.Exactly(0).Items);
        }

        [Test]
        public void TestImeCompositionInvokesEvent()
        {
            startComposition();
            AddAssert("ime composition active", () => textBox.ImeCompositionActive);
        }

        [Test]
        public void TestImeResultInvokesEvent()
        {
            startComposition();

            AddStep("trigger result", () => textInput.TriggerImeResult(composition_text));
            AddAssert("ime result event raised", () => textBox.ImeResultQueue.Dequeue().Equals(new ImeResultEvent
            {
                Result = composition_text,
                Successful = true
            }));
            assertCompositionNotActive();
        }

        [Test]
        public void TestPressingKeysDuringImeCompositionOnlyInvokesCompositionEvent()
        {
            startComposition();

            AddAssert("ime composition active", () => textBox.ImeCompositionActive);

            AddStep("press left arrow and move selection in composition", () =>
            {
                textInput.TriggerImeComposition(composition_text, composition_text.Length - 1, 0);
                InputManager.Keys(PlatformAction.MoveBackwardChar);
            });
            AddAssert("ime composition event raised", () => textBox.ImeCompositionQueue.Dequeue().Equals(new ImeCompositionEvent
            {
                NewComposition = composition_text,
                AddedTextLength = 0,
                RemovedTextLength = 0,
                SelectionMoved = true
            }));
            AddAssert("caret moved event not raised", () => textBox.CaretMovedQueue.Count == 0);

            AddStep("press backspace to delete character in composition", () =>
            {
                textInput.TriggerImeComposition(composition_text[..^1], composition_text.Length - 2, 0);
                InputManager.Keys(PlatformAction.DeleteBackwardChar);
            });
            AddAssert("ime composition event raised", () => textBox.ImeCompositionQueue.Dequeue().Equals(new ImeCompositionEvent
            {
                NewComposition = composition_text[..^1],
                AddedTextLength = 0,
                RemovedTextLength = 1,
                SelectionMoved = true
            }));
            AddAssert("user text removed event not raised", () => textBox.UserRemovedTextQueue.Count == 0);

            AddStep("press enter and complete composition", () =>
            {
                textInput.TriggerImeResult(composition_text);
                InputManager.Key(Key.Enter);
            });
            AddAssert("ime result event raised", () => textBox.ImeResultQueue.Dequeue().Equals(new ImeResultEvent
            {
                Result = composition_text,
                Successful = true
            }));
            AddAssert("text committed event not raised", () => textBox.CommittedTextQueue.Count == 0);

            AddStep("press enter after composition finished", () => InputManager.Key(Key.Enter));
            AddAssert("text committed event raised", () => textBox.CommittedTextQueue.Dequeue());
        }

        [Test]
        public void TestInvalidCompositionInvokesInputError()
        {
            startComposition();

            AddStep("trigger composition with invalid selection start", () =>
            {
                textInput.TriggerImeComposition(composition_text, composition_text.Length + 1, 0);
            });
            AddAssert("ime composition event raised", () => textBox.ImeCompositionQueue.Dequeue().Equals(new ImeCompositionEvent
            {
                NewComposition = composition_text,
                AddedTextLength = 0,
                RemovedTextLength = 0,
                SelectionMoved = false
            }));
            AddAssert("input error event raised", () => textBox.InputErrorQueue.Dequeue());

            AddStep("trigger composition with invalid selection length", () =>
            {
                textInput.TriggerImeComposition(composition_text, composition_text.Length - 1, 2);
            });
            AddAssert("ime composition event raised", () => textBox.ImeCompositionQueue.Dequeue().Equals(new ImeCompositionEvent
            {
                NewComposition = composition_text,
                AddedTextLength = 0,
                RemovedTextLength = 0,
                SelectionMoved = true
            }));
            AddAssert("input error event raised", () => textBox.InputErrorQueue.Dequeue());
        }

        [Test]
        public void TestEmptyCompositionDoesntInvokeEvent()
        {
            AddStep("trigger empty composition", () => textInput.TriggerImeComposition(string.Empty, 0, 0));
            assertCompositionNotActive();
            testNormalTextInput();
        }

        [Test]
        public void TestCancellingCompositionInvokesEvent()
        {
            startComposition();

            AddStep("trigger empty composition", () =>
            {
                textInput.TriggerImeComposition(string.Empty, 0, 0);
                InputManager.Key(Key.Escape);
            });
            AddAssert("ime composition event raised", () => textBox.ImeCompositionQueue.Dequeue().Equals(new ImeCompositionEvent
            {
                NewComposition = string.Empty,
                AddedTextLength = 0,
                RemovedTextLength = composition_text.Length,
                SelectionMoved = true
            }));
            assertCompositionNotActive();
            AddAssert("text box focused", () => textBox.HasFocus);

            testNormalTextInput();

            AddStep("press escape again to kill focus", () => InputManager.Key(Key.Escape));
            AddAssert("text box not focused", () => textBox.HasFocus == false);
            AddAssert("text committed event raised", () => textBox.CommittedTextQueue.Dequeue() && textBox.CommittedTextQueue.Count == 0);
            AddAssert("input deactivated", () => textInput.DeactivationQueue.Dequeue() && textInput.DeactivationQueue.Count == 0);
        }

        [Test]
        public void TestLengthLimitTruncatesComposition()
        {
            AddStep("set length limit", () => textBox.LengthLimit = default_text.Length + composition_text.Length - 1);

            AddStep("start composition", () => textInput.TriggerImeComposition(composition_text, composition_text.Length, 0));
            AddAssert("truncated ime composition event raised", () => textBox.ImeCompositionQueue.Dequeue().Equals(new ImeCompositionEvent
            {
                NewComposition = composition_text[..^1],
                AddedTextLength = composition_text.Length - 1,
                RemovedTextLength = 0,
                SelectionMoved = true
            }));
            AddAssert("input error event raised", () => textBox.InputErrorQueue.Dequeue());
        }

        [Test]
        public void TestInvalidCharactersRemovedFromComposition()
        {
            const string invalid_composition = "12\t34";
            const string valid_composition = "1234";

            AddStep("start composition with invalid characters", () => textInput.TriggerImeComposition(invalid_composition, invalid_composition.Length, 0));
            AddAssert("ime composition event with valid composition raised", () => textBox.ImeCompositionQueue.Dequeue().Equals(new ImeCompositionEvent
            {
                NewComposition = valid_composition,
                AddedTextLength = valid_composition.Length,
                RemovedTextLength = 0,
                SelectionMoved = true
            }));
            AddAssert("input error event raised", () => textBox.InputErrorQueue.Dequeue());
        }

        [Test]
        public void TestSettingTextFinalizesComposition()
        {
            const string new_text = "some new text";

            startComposition();

            AddStep("set text", () => textBox.Text = new_text);
            assertCompositionNotActive();
            AddAssert("text is expected", () => textBox.Text == new_text);
        }

        [Test]
        public void TestSettingCurrentFinalizesComposition()
        {
            const string new_text = "some new text";

            startComposition();

            AddStep("set current", () => textBox.Current.Value = new_text);
            assertCompositionNotActive();
            AddAssert("current value matches expected", () => textBox.Current.Value == new_text);
        }

        [Test]
        public void TestDisablingCurrentStopsComposition()
        {
            startComposition();

            AddStep("disable current", () => textBox.Current.Disabled = true);
            assertCompositionNotActive();
            AddAssert("current value matches expected", () => textBox.Current.Value == default_text + composition_text);

            AddStep("trigger composition", () => textInput.TriggerImeComposition(composition_text, composition_text.Length, 0));
            assertCompositionNotActive();
            AddAssert("input error event raised", () => textBox.InputErrorQueue.Dequeue());
        }

        [Test]
        public void TestReadOnlyTextBoxDoesntReceiveInput()
        {
            startComposition();

            AddStep("set read only", () => textBox.ReadOnly = true);

            AddAssert("text committed event raised", () => textBox.CommittedTextQueue.Dequeue() && textBox.CommittedTextQueue.Count == 0);
            AddAssert("input deactivated", () => textInput.DeactivationQueue.Dequeue() && textInput.DeactivationQueue.Count == 0);
            assertCompositionNotActive();

            AddStep("trigger composition", () => textInput.TriggerImeComposition(composition_text, composition_text.Length, 0));
            assertCompositionNotActive();
            AddStep("press key to insert normal text", () =>
            {
                textInput.Text("W");
                InputManager.Key(Key.W);
            });
            AddAssert("user text consumed event not raised", () => textBox.UserConsumedTextQueue.Count == 0);
        }

        [Test]
        public void TestStartingCompositionRemovesSelection()
        {
            AddStep("select all text", () => InputManager.Keys(PlatformAction.SelectAll));
            AddAssert("text selection event (all)", () => textBox.TextSelectionQueue.Dequeue() == TextBox.TextSelectionType.All);

            startComposition();
            AddAssert("user text removed event not raised", () => textBox.UserRemovedTextQueue.Count == 0);
            AddAssert("text matches expected", () => textBox.Text == composition_text);
        }

        /// <summary>
        /// Tests that changing focus directly between two <see cref="TextBox"/>es doesn't deactivate and reactivate text input,
        /// as that creates bad UX with mobile virtual keyboards.
        /// </summary>
        [TestCase(false)]
        [TestCase(true)]
        public void TestChangingFocusDoesNotReactivate(bool allowIme)
        {
            EventQueuesTextBox secondTextBox = null;

            AddStep("add second textbox", () => textInputContainer.Add(secondTextBox = new EventQueuesTextBox
            {
                ImeAllowed = allowIme,
                Anchor = Anchor.CentreLeft,
                Origin = Anchor.CentreLeft,
                CommitOnFocusLost = true,
                Size = new Vector2(200, 40),
                Text = default_text,
            }));

            AddStep("focus second textbox", () =>
            {
                InputManager.MoveMouseTo(secondTextBox);
                InputManager.Click(MouseButton.Left);
            });
            AddStep("dequeue commit event", () => textBox.CommittedTextQueue.Dequeue());

            AddAssert("text input not deactivated", () => textInput.DeactivationQueue.Count == 0);
            AddAssert("text input not activated again", () => textInput.ActivationQueue.Count == 0);
            AddAssert($"text input ensure activated {(allowIme ? "with" : "without")} IME", () => textInput.EnsureActivatedQueue.Dequeue() == allowIme && textInput.EnsureActivatedQueue.Count == 0);

            AddStep("commit text", () => InputManager.Key(Key.Enter));
            AddAssert("text input deactivated", () => textInput.DeactivationQueue.Dequeue());
        }

        [TearDownSteps]
        public void TearDownSteps()
        {
            AddAssert("all event queues emptied", () => textBox.InputErrorQueue.Count == 0 &&
                                                        textBox.UserConsumedTextQueue.Count == 0 &&
                                                        textBox.UserRemovedTextQueue.Count == 0 &&
                                                        textBox.CommittedTextQueue.Count == 0 &&
                                                        textBox.CaretMovedQueue.Count == 0 &&
                                                        textBox.ImeCompositionQueue.Count == 0 &&
                                                        textBox.ImeResultQueue.Count == 0 &&
                                                        textBox.TextSelectionQueue.Count == 0 &&
                                                        textBox.TextDeselectionQueue.Count == 0 &&
                                                        textInput.ActivationQueue.Count == 0 &&
                                                        textInput.DeactivationQueue.Count == 0 &&
                                                        textInput.EnsureActivatedQueue.Count == 0);
        }

        private void assertCompositionNotActive()
        {
            AddAssert("ime composition not active", () => !textBox.ImeCompositionActive);
            AddAssert("ime composition event not raised", () => textBox.ImeCompositionQueue.Count == 0);
        }

        private void startComposition()
        {
            AddStep("start composition", () =>
            {
                textInput.TriggerImeComposition(composition_text, composition_text.Length, 0);
                InputManager.Key(Key.T);
            });
            AddAssert("ime composition event raised", () => textBox.ImeCompositionQueue.Dequeue().Equals(new ImeCompositionEvent
            {
                NewComposition = composition_text,
                AddedTextLength = composition_text.Length,
                RemovedTextLength = 0,
                SelectionMoved = true
            }));
        }

        private void testNormalTextInput()
        {
            AddStep("press key to insert normal text", () =>
            {
                textInput.Text("W");
                InputManager.Key(Key.W);
            });
            AddAssert("user text consumed event raised", () => textBox.UserConsumedTextQueue.Dequeue() == "W" && textBox.UserConsumedTextQueue.Count == 0);
        }

        public class EventQueuesTextBox : TestSceneTextBox.InsertableTextBox
        {
            public bool ImeAllowed { get; set; } = true;

            protected override bool AllowIme => ImeAllowed;

            public readonly Queue<bool> InputErrorQueue = new Queue<bool>();
            public readonly Queue<string> UserConsumedTextQueue = new Queue<string>();
            public readonly Queue<string> UserRemovedTextQueue = new Queue<string>();
            public readonly Queue<bool> CommittedTextQueue = new Queue<bool>();
            public readonly Queue<bool> CaretMovedQueue = new Queue<bool>();
            public readonly Queue<ImeCompositionEvent> ImeCompositionQueue = new Queue<ImeCompositionEvent>();
            public readonly Queue<ImeResultEvent> ImeResultQueue = new Queue<ImeResultEvent>();
            public readonly Queue<TextSelectionType> TextSelectionQueue = new Queue<TextSelectionType>();
            public readonly Queue<bool> TextDeselectionQueue = new Queue<bool>();

            protected override void NotifyInputError() => InputErrorQueue.Enqueue(true);
            protected override void OnUserTextAdded(string consumed) => UserConsumedTextQueue.Enqueue(consumed);
            protected override void OnUserTextRemoved(string removed) => UserRemovedTextQueue.Enqueue(removed);
            protected override void OnTextCommitted(bool textChanged) => CommittedTextQueue.Enqueue(textChanged);
            protected override void OnCaretMoved(bool selecting) => CaretMovedQueue.Enqueue(selecting);

            protected override void OnImeComposition(string newComposition, int removedTextLength, int addedTextLength, bool selectionMoved) =>
                ImeCompositionQueue.Enqueue(new ImeCompositionEvent
                {
                    NewComposition = newComposition,
                    RemovedTextLength = removedTextLength,
                    AddedTextLength = addedTextLength,
                    SelectionMoved = selectionMoved
                });

            protected override void OnImeResult(string result, bool successful) =>
                ImeResultQueue.Enqueue(new ImeResultEvent
                {
                    Result = result,
                    Successful = successful
                });

            protected override void OnTextSelectionChanged(TextSelectionType selectionType) => TextSelectionQueue.Enqueue(selectionType);
            protected override void OnTextDeselected() => TextDeselectionQueue.Enqueue(true);

            public new bool ImeCompositionActive => base.ImeCompositionActive;
        }

        public class ManualTextInputContainer : Container
        {
            [Cached(typeof(TextInputSource))]
            public readonly ManualTextInput TextInput;

            public ManualTextInputContainer()
            {
                RelativeSizeAxes = Axes.Both;
                TextInput = new ManualTextInput();
            }
        }

        public class ManualTextInput : TextInputSource
        {
            public void Text(string text) => TriggerTextInput(text);

            public new void TriggerImeComposition(string text, int start, int length)
            {
                base.TriggerImeComposition(text, start, length);
            }

            public new void TriggerImeResult(string text)
            {
                base.TriggerImeResult(text);
            }

            public override void ResetIme()
            {
                base.ResetIme();

                // this call will be somewhat delayed in a real world scenario, but let's run it immediately for simplicity.
                base.TriggerImeComposition(string.Empty, 0, 0);
            }

            public readonly Queue<bool> ActivationQueue = new Queue<bool>();
            public readonly Queue<bool> EnsureActivatedQueue = new Queue<bool>();
            public readonly Queue<bool> DeactivationQueue = new Queue<bool>();

            protected override void ActivateTextInput(bool allowIme)
            {
                base.ActivateTextInput(allowIme);
                ActivationQueue.Enqueue(allowIme);
            }

            protected override void EnsureTextInputActivated(bool allowIme)
            {
                base.EnsureTextInputActivated(allowIme);
                EnsureActivatedQueue.Enqueue(allowIme);
            }

            protected override void DeactivateTextInput()
            {
                base.DeactivateTextInput();
                DeactivationQueue.Enqueue(true);
            }
        }

        public struct ImeCompositionEvent
        {
            public string NewComposition;
            public int RemovedTextLength;
            public int AddedTextLength;
            public bool SelectionMoved;

            public bool Equals(ImeCompositionEvent other) => NewComposition == other.NewComposition &&
                                                             RemovedTextLength == other.RemovedTextLength &&
                                                             AddedTextLength == other.AddedTextLength &&
                                                             SelectionMoved == other.SelectionMoved;
        }

        public struct ImeResultEvent
        {
            public string Result;
            public bool Successful;

            public bool Equals(ImeResultEvent other) => Result == other.Result &&
                                                        Successful == other.Successful;
        }
    }
}
