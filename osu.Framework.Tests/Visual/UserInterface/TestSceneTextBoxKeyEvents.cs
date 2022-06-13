// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Input;
using osu.Framework.Input.Events;
using osu.Framework.Platform;
using osu.Framework.Testing;
using osuTK;
using osuTK.Input;

namespace osu.Framework.Tests.Visual.UserInterface
{
    public class TestSceneTextBoxKeyEvents : ManualInputManagerTestScene
    {
        private KeyEventQueuesTextBox textBox;

        private TestSceneTextBoxEvents.ManualTextInput textInput;

        [Resolved]
        private GameHost host { get; set; }

        private const string default_text = "default text";

        [SetUpSteps]
        public void SetUpSteps()
        {
            TestSceneTextBoxEvents.ManualTextInputContainer textInputContainer = null;

            AddStep("add manual text input container", () =>
            {
                Child = textInputContainer = new TestSceneTextBoxEvents.ManualTextInputContainer();
                textInput = textInputContainer.TextInput;
            });

            AddStep("add textbox", () => textInputContainer.Child = textBox = new KeyEventQueuesTextBox
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
            assertKeyEventsNotConsumedForAction(PlatformAction.MoveForwardLine);
            AddStep("dequeue caret event", () => textBox.CaretMovedQueue.Dequeue());
        }

        [Test]
        public void TestTextInputWithoutKeyPress()
        {
            AddStep("insert text", () => textInput.Text("W"));
            AddAssert("user text consumed event", () => textBox.UserConsumedTextQueue.Dequeue() == "W" && textBox.UserConsumedTextQueue.Count == 0);
        }

        [Test]
        public void TestTextInputConsumesKeyDown()
        {
            AddStep("press key to insert text", () =>
            {
                textInput.Text("W");
                InputManager.Key(Key.W);
            });
            AddAssert("KeyDown event consumed", () => textBox.KeyDownQueue.Dequeue() && textBox.KeyDownQueue.Count == 0);
            AddAssert("user text consumed event", () => textBox.UserConsumedTextQueue.Dequeue() == "W" && textBox.UserConsumedTextQueue.Count == 0);
        }

        [Test]
        public void TestKeyRepeat()
        {
            AddStep("press and hold key to insert text", () =>
            {
                textInput.Text("W");
                InputManager.PressKey(Key.W);
            });
            AddAssert("KeyDown event consumed", () => textBox.KeyDownQueue.Dequeue() && textBox.KeyDownQueue.Count == 0);
            AddAssert("user text consumed event", () => textBox.UserConsumedTextQueue.Dequeue() == "W" && textBox.UserConsumedTextQueue.Count == 0);

            for (int i = 0; i < 3; i++)
            {
                AddStep("text input from repeat", () => textInput.Text("W"));
                AddAssert("user text consumed event", () => textBox.UserConsumedTextQueue.Dequeue() == "W" && textBox.UserConsumedTextQueue.Count == 0);
            }

            AddStep("release key", () => InputManager.ReleaseKey(Key.W));
            AddAssert("key repeated", () => textBox.KeyDownRepeatQueue.Count != 0);
            assertAllRepeatKeyDownEventsConsumed();
        }

        [Test]
        public void TestTwoKeys()
        {
            AddStep("press and hold first key to insert text", () =>
            {
                textInput.Text("F");
                InputManager.PressKey(Key.F);
            });
            AddAssert("KeyDown event consumed", () => textBox.KeyDownQueue.Dequeue() && textBox.KeyDownQueue.Count == 0);
            AddAssert("user text consumed event", () => textBox.UserConsumedTextQueue.Dequeue() == "F" && textBox.UserConsumedTextQueue.Count == 0);

            AddStep("press second key to insert text", () =>
            {
                textInput.Text("S");
                InputManager.Key(Key.S);
            });
            AddAssert("KeyDown event consumed", () => textBox.KeyDownQueue.Dequeue() && textBox.KeyDownQueue.Count == 0);
            AddAssert("user text consumed event", () => textBox.UserConsumedTextQueue.Dequeue() == "S" && textBox.UserConsumedTextQueue.Count == 0);

            AddStep("press a key (no text input)", () => InputManager.Key(Key.F1));
            AddAssert("KeyDown event consumed", () => textBox.KeyDownQueue.Dequeue() && textBox.KeyDownQueue.Count == 0);

            AddStep("release the first key", () => InputManager.ReleaseKey(Key.F));
            AddAssert("key repeated", () => textBox.KeyDownRepeatQueue.Count != 0);

            assertAllRepeatKeyDownEventsConsumed();
        }

        [Test]
        public void TestDeleteWorksDuringTextInput()
        {
            AddStep("press and hold key to insert text", () =>
            {
                textInput.Text("W");
                InputManager.PressKey(Key.W);
            });
            AddAssert("KeyDown event consumed", () => textBox.KeyDownQueue.Dequeue() && textBox.KeyDownQueue.Count == 0);
            AddAssert("user text consumed event", () => textBox.UserConsumedTextQueue.Dequeue() == "W" && textBox.UserConsumedTextQueue.Count == 0);

            AddStep("invoke delete action to remove text", () => InputManager.Keys(PlatformAction.DeleteBackwardChar));
            assertKeyEventsNotConsumedForAction(PlatformAction.DeleteBackwardChar);
            AddAssert("user text removed event", () => textBox.UserRemovedTextQueue.Dequeue() == "W" && textBox.UserRemovedTextQueue.Count == 0);

            AddStep("release key", () => InputManager.ReleaseKey(Key.W));
            AddAssert("key repeated", () => textBox.KeyDownRepeatQueue.Count != 0);
            assertAllRepeatKeyDownEventsConsumed();
        }

        [Test]
        public void TestEscapeWorksDuringTextInput()
        {
            AddStep("press and hold key to insert text", () =>
            {
                textInput.Text("W");
                InputManager.PressKey(Key.W);
            });
            AddAssert("KeyDown event consumed", () => textBox.KeyDownQueue.Dequeue() && textBox.KeyDownQueue.Count == 0);
            AddAssert("user text consumed event", () => textBox.UserConsumedTextQueue.Dequeue() == "W" && textBox.UserConsumedTextQueue.Count == 0);

            AddStep("press escape to kill focus", () => InputManager.Key(Key.Escape));
            AddAssert("KeyDown event consumed", () => textBox.KeyDownQueue.Dequeue() && textBox.KeyDownQueue.Count == 0);
            AddAssert("text committed event", () => textBox.CommittedTextQueue.Dequeue() && textBox.CommittedTextQueue.Count == 0);
            AddAssert("text box not focused", () => !textBox.HasFocus);

            AddStep("focus textbox again", () =>
            {
                InputManager.MoveMouseTo(textBox);
                InputManager.Click(MouseButton.Left);
            });

            AddStep("press a key (no text input)", () => InputManager.Key(Key.F1));
            AddAssert("KeyDown event not consumed", () => !textBox.KeyDownQueue.Dequeue() && textBox.KeyDownQueue.Count == 0);

            AddStep("release key", () => InputManager.ReleaseKey(Key.W));
            AddAssert("key repeated", () => textBox.KeyDownRepeatQueue.Count != 0);
            assertAllRepeatKeyDownEventsConsumed();

            AddStep("press key to insert text", () =>
            {
                textInput.Text("W");
                InputManager.Key(Key.W);
            });
            AddAssert("KeyDown event consumed", () => textBox.KeyDownQueue.Dequeue() && textBox.KeyDownQueue.Count == 0);
            AddAssert("user text consumed event", () => textBox.UserConsumedTextQueue.Dequeue() == "W" && textBox.UserConsumedTextQueue.Count == 0);
        }

        [Test]
        public void TestReleaseAndPressKeysInSameFrame()
        {
            AddStep("press and hold first key to insert text", () =>
            {
                textInput.Text("F");
                InputManager.PressKey(Key.F);
            });
            AddAssert("KeyDown event consumed", () => textBox.KeyDownQueue.Dequeue() && textBox.KeyDownQueue.Count == 0);
            AddAssert("user text consumed event", () => textBox.UserConsumedTextQueue.Dequeue() == "F" && textBox.UserConsumedTextQueue.Count == 0);

            AddStep("release first key, press second key, and send text", () =>
            {
                InputManager.ReleaseKey(Key.F);
                textInput.Text("S");
                InputManager.PressKey(Key.S);
            });
            AddAssert("KeyDown event consumed", () => textBox.KeyDownQueue.Dequeue() && textBox.KeyDownQueue.Count == 0);
            AddAssert("user text consumed event", () => textBox.UserConsumedTextQueue.Dequeue() == "S" && textBox.UserConsumedTextQueue.Count == 0);

            AddStep("release key", () => InputManager.ReleaseKey(Key.S));
            AddAssert("keys repeated", () => textBox.KeyDownRepeatQueue.Count != 0);
            assertAllRepeatKeyDownEventsConsumed();
        }

        [TearDownSteps]
        public void TearDownSteps()
        {
            AddStep("press a key", () => InputManager.Key(Key.F1));
            AddAssert("KeyDown event not consumed", () => !textBox.KeyDownQueue.Dequeue() && textBox.KeyDownQueue.Count == 0);

            AddAssert("all event queues emptied", () => textBox.InputErrorQueue.Count == 0 &&
                                                        textBox.UserConsumedTextQueue.Count == 0 &&
                                                        textBox.UserRemovedTextQueue.Count == 0 &&
                                                        textBox.CommittedTextQueue.Count == 0 &&
                                                        textBox.CaretMovedQueue.Count == 0 &&
                                                        textBox.ImeCompositionQueue.Count == 0 &&
                                                        textBox.ImeResultQueue.Count == 0 &&
                                                        textBox.KeyDownQueue.Count == 0 &&
                                                        textBox.PlatformActionQueue.Count == 0 &&
                                                        textBox.KeyDownRepeatQueue.Count == 0);
        }

        private void assertKeyEventsNotConsumedForAction(PlatformAction action)
        {
            AddAssert("KeyDown events not consumed", () =>
            {
                // mirrored from ManualInputManager.Keys(PlatformAction)
                var binding = host.PlatformKeyBindings.First(b => (PlatformAction)b.Action == action);

                for (int i = 0; i < binding.KeyCombination.Keys.Length; i++)
                {
                    // fail if consumed
                    if (textBox.KeyDownQueue.Dequeue())
                        return false;
                }

                return textBox.KeyDownQueue.Count == 0;
            });

            AddAssert("PlatformAction event consumed", () => textBox.PlatformActionQueue.Dequeue() && textBox.PlatformActionQueue.Count == 0);
        }

        private void assertAllRepeatKeyDownEventsConsumed()
        {
            AddAssert("all repeat KeyDown events consumed", () =>
            {
                int n = textBox.KeyDownRepeatQueue.Count;

                for (int i = 0; i < n; i++)
                {
                    if (!textBox.KeyDownRepeatQueue.Dequeue())
                        return false;
                }

                return textBox.KeyDownRepeatQueue.Count == 0;
            });
        }

        private class KeyEventQueuesTextBox : TestSceneTextBoxEvents.EventQueuesTextBox
        {
            public readonly Queue<bool> KeyDownQueue = new Queue<bool>();
            public readonly Queue<bool> KeyDownRepeatQueue = new Queue<bool>();
            public readonly Queue<bool> PlatformActionQueue = new Queue<bool>();

            protected override bool OnKeyDown(KeyDownEvent e)
            {
                bool consumed = base.OnKeyDown(e);

                if (e.Repeat)
                    KeyDownRepeatQueue.Enqueue(consumed);
                else
                    KeyDownQueue.Enqueue(consumed);

                return consumed;
            }

            public override bool OnPressed(KeyBindingPressEvent<PlatformAction> e)
            {
                bool consumed = base.OnPressed(e);
                PlatformActionQueue.Enqueue(consumed);
                return consumed;
            }
        }
    }
}
