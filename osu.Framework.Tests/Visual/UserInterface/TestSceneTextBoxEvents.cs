// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Containers;
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

        private const string default_text = "some default text";
        private const string composition_text = "test";

        [SetUpSteps]
        public void SetUpSteps()
        {
            ManualTextInputContainer textInputContainer = null;

            AddStep("add manual text input container", () =>
            {
                Child = textInputContainer = new ManualTextInputContainer();
                textInput = textInputContainer.TextInput;
            });

            AddStep("add textbox", () => textInputContainer.Child = textBox = new EventQueuesTextBox
            {
                CommitOnFocusLost = true,
                ReleaseFocusOnCommit = false,
                Size = new Vector2(200, 40),
                Text = default_text,
            });

            AddStep("focus textbox", () =>
            {
                InputManager.MoveMouseTo(textBox);
                InputManager.Click(MouseButton.Left);
            });

            AddStep("move caret to end", () => InputManager.Keys(PlatformAction.MoveForwardLine));
            AddStep("dequeue caret event", () => textBox.CaretMovedQueue.Dequeue());
        }

        [Test]
        public void TestCommitIsNewTextSecondTime()
        {
            AddStep("add handler to reset on commit", () => textBox.OnCommit += (sender, isNew) =>
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
                // press a key so TextBox starts consuming text
                InputManager.Key(Key.W);
                textInput.AddToPendingText("W");
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
        }

        [Test]
        public void TestMovingOrExpandingSelectionInvokesEvent()
        {
            AddStep("invoke move action to move caret", () => InputManager.Keys(PlatformAction.MoveBackwardLine));
            AddAssert("caret moved event", () =>
                // Ensure dequeued caret move event has selecting = false.
                textBox.CaretMovedQueue.Dequeue() == false && textBox.CommittedTextQueue.Count == 0);

            AddStep("invoke select action to expand selection", () => InputManager.Keys(PlatformAction.SelectForwardChar));
            AddAssert("caret moved event", () =>
                // Ensure dequeued caret move event has selecting = true.
                textBox.CaretMovedQueue.Dequeue() && textBox.CommittedTextQueue.Count == 0);
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
            assertCompositionNotActive();

            AddStep("trigger composition", () => textInput.TriggerImeComposition(composition_text, composition_text.Length, 0));
            assertCompositionNotActive();
            AddStep("press key to insert normal text", () =>
            {
                InputManager.Key(Key.W);
                textInput.AddToPendingText("W");
            });
            AddAssert("user text consumed event not raised", () => textBox.UserConsumedTextQueue.Count == 0);
        }

        [Test]
        public void TestStartingCompositionRemovesSelection()
        {
            AddStep("select all text", () => InputManager.Keys(PlatformAction.SelectAll));

            startComposition();
            AddAssert("user text removed event not raised", () => textBox.UserRemovedTextQueue.Count == 0);
            AddAssert("text matches expected", () => textBox.Text == composition_text);
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
                                                        textBox.ImeResultQueue.Count == 0);
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
                InputManager.Key(Key.W);
                textInput.AddToPendingText("W");
            });
            AddAssert("user text consumed event raised", () => textBox.UserConsumedTextQueue.Dequeue() == "W" && textBox.UserConsumedTextQueue.Count == 0);
        }

        private class EventQueuesTextBox : TestSceneTextBox.InsertableTextBox
        {
            public readonly Queue<bool> InputErrorQueue = new Queue<bool>();
            public readonly Queue<string> UserConsumedTextQueue = new Queue<string>();
            public readonly Queue<string> UserRemovedTextQueue = new Queue<string>();
            public readonly Queue<bool> CommittedTextQueue = new Queue<bool>();
            public readonly Queue<bool> CaretMovedQueue = new Queue<bool>();
            public readonly Queue<ImeCompositionEvent> ImeCompositionQueue = new Queue<ImeCompositionEvent>();
            public readonly Queue<ImeResultEvent> ImeResultQueue = new Queue<ImeResultEvent>();

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

            public new bool ImeCompositionActive => base.ImeCompositionActive;
        }

        private class ManualTextInputContainer : Container
        {
            [Cached(typeof(TextInputSource))]
            public readonly ManualTextInput TextInput;

            public ManualTextInputContainer()
            {
                TextInput = new ManualTextInput();
            }
        }

        private class ManualTextInput : TextInputSource
        {
            public void AddToPendingText(string text) => AddPendingText(text);

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
        }

        private struct ImeCompositionEvent
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

        private struct ImeResultEvent
        {
            public string Result;
            public bool Successful;

            public bool Equals(ImeResultEvent other) => Result == other.Result &&
                                                        Successful == other.Successful;
        }
    }
}
