// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Input;
using osu.Framework.Testing;
using osuTK;
using osuTK.Input;

namespace osu.Framework.Tests.Visual.UserInterface
{
    public class TestSceneTextBoxEvents : ManualInputManagerTestScene
    {
        private EventQueuesTextBox textBox;

        private const string default_text = "some default text";

        [SetUpSteps]
        public void SetUpSteps()
        {
            AddStep("add textbox", () => Child = textBox = new EventQueuesTextBox
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
        [Ignore("not possible to test yet for attached reason.")]
        public void TestInsertingUserTextInvokesEvent()
        {
            // todo: this is not straightforward to test at the moment (requires manipulating ITextInputSource, which is stored at host level), steps commented for now.
            //AddStep("press letter key to insert text", () => addToPendingTextInput());
            //AddAssert("user text consumed event", () => textBox.UserConsumedTextQueue.Dequeue() == "W" && textBox.UserConsumedTextQueue.Count == 0);
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

        [TearDownSteps]
        public void TearDownSteps()
        {
            AddAssert("all event queues emptied", () => textBox.UserConsumedTextQueue.Count == 0 &&
                                                        textBox.UserRemovedTextQueue.Count == 0 &&
                                                        textBox.CommittedTextQueue.Count == 0 &&
                                                        textBox.CaretMovedQueue.Count == 0);
        }

        private class EventQueuesTextBox : TestSceneTextBox.InsertableTextBox
        {
            public readonly Queue<string> UserConsumedTextQueue = new Queue<string>();
            public readonly Queue<string> UserRemovedTextQueue = new Queue<string>();
            public readonly Queue<bool> CommittedTextQueue = new Queue<bool>();
            public readonly Queue<bool> CaretMovedQueue = new Queue<bool>();

            protected override void OnUserTextAdded(string consumed) => UserConsumedTextQueue.Enqueue(consumed);
            protected override void OnUserTextRemoved(string removed) => UserRemovedTextQueue.Enqueue(removed);
            protected override void OnTextCommitted(bool textChanged) => CommittedTextQueue.Enqueue(textChanged);
            protected override void OnCaretMoved(bool selecting) => CaretMovedQueue.Enqueue(selecting);
        }
    }
}
