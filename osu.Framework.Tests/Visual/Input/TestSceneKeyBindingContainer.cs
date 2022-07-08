// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Framework.Testing;
using osuTK;
using osuTK.Input;

namespace osu.Framework.Tests.Visual.Input
{
    [HeadlessTest]
    public class TestSceneKeyBindingContainer : ManualInputManagerTestScene
    {
        [Test]
        public void TestTriggerWithNoKeyBindings()
        {
            bool pressedReceived = false;
            bool releasedReceived = false;

            TestKeyBindingContainer keyBindingContainer = null;

            AddStep("add container", () =>
            {
                pressedReceived = false;
                releasedReceived = false;

                Child = keyBindingContainer = new TestKeyBindingContainer
                {
                    Child = new TestKeyBindingReceptor
                    {
                        Pressed = _ => pressedReceived = true,
                        Released = _ => releasedReceived = true
                    }
                };
            });

            AddStep("trigger press", () => keyBindingContainer.TriggerPressed(TestAction.ActionA));
            AddAssert("press received", () => pressedReceived);

            AddStep("trigger release", () => keyBindingContainer.TriggerReleased(TestAction.ActionA));
            AddAssert("release received", () => releasedReceived);
        }

        [Test]
        public void TestPressKeyBeforeKeyBindingContainerAdded()
        {
            List<TestAction> pressedActions = new List<TestAction>();
            List<TestAction> releasedActions = new List<TestAction>();

            AddStep("press key B", () => InputManager.PressKey(Key.B));

            AddStep("add container", () =>
            {
                pressedActions.Clear();
                releasedActions.Clear();

                Child = new TestKeyBindingContainer
                {
                    Child = new TestKeyBindingReceptor
                    {
                        Pressed = a => pressedActions.Add(a),
                        Released = a => releasedActions.Add(a)
                    }
                };
            });

            AddStep("press key A", () => InputManager.PressKey(Key.A));
            AddAssert("only one action triggered", () => pressedActions.Count == 1);
            AddAssert("ActionA triggered", () => pressedActions[0] == TestAction.ActionA);
            AddAssert("no actions released", () => releasedActions.Count == 0);

            AddStep("release key A", () => InputManager.ReleaseKey(Key.A));
            AddAssert("only one action triggered", () => pressedActions.Count == 1);
            AddAssert("only one action released", () => releasedActions.Count == 1);
            AddAssert("ActionA released", () => releasedActions[0] == TestAction.ActionA);
        }

        [Test]
        public void TestKeyHandledByOtherDrawableDoesNotTrigger()
        {
            List<TestAction> pressedActions = new List<TestAction>();
            List<TestAction> releasedActions = new List<TestAction>();

            TextBox textBox = null;

            AddStep("add children", () =>
            {
                pressedActions.Clear();
                releasedActions.Clear();

                Child = new TestKeyBindingContainer
                {
                    Children = new Drawable[]
                    {
                        textBox = new BasicTextBox
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Size = new Vector2(200, 30)
                        },
                        new TestKeyBindingReceptor
                        {
                            Pressed = a => pressedActions.Add(a),
                            Released = a => releasedActions.Add(a)
                        }
                    }
                };
            });

            AddStep("focus textbox and move mouse away", () =>
            {
                InputManager.MoveMouseTo(textBox);
                InputManager.Click(MouseButton.Left);
                InputManager.MoveMouseTo(textBox, new Vector2(0, 100));
            });

            AddStep("press enter", () => InputManager.PressKey(Key.Enter));
            AddStep("press mouse button", () => InputManager.PressButton(MouseButton.Left));

            AddStep("release enter", () => InputManager.ReleaseKey(Key.Enter));
            AddStep("release mouse button", () => InputManager.ReleaseButton(MouseButton.Left));

            AddAssert("no pressed actions", () => pressedActions.Count == 0);
            AddAssert("no released actions", () => releasedActions.Count == 0);
        }

        [Test]
        public void TestReleasingSpecificModifierDoesNotReleaseCommonBindingIfOtherKeyIsActive()
        {
            bool pressedReceived = false;
            bool releasedReceived = false;

            AddStep("add container", () =>
            {
                pressedReceived = false;
                releasedReceived = false;

                Child = new TestKeyBindingContainer
                {
                    Child = new TestKeyBindingReceptor
                    {
                        Pressed = _ => pressedReceived = true,
                        Released = _ => releasedReceived = true
                    }
                };
            });

            AddStep("press lctrl", () => InputManager.PressKey(Key.LControl));
            AddAssert("press received", () => pressedReceived);

            AddStep("reset variables", () =>
            {
                pressedReceived = false;
                releasedReceived = false;
            });

            AddStep("press rctrl", () => InputManager.PressKey(Key.RControl));
            AddAssert("press not received", () => !pressedReceived);
            AddAssert("release not received", () => !releasedReceived);

            AddStep("release rctrl", () => InputManager.ReleaseKey(Key.RControl));
            AddAssert("release not received", () => !releasedReceived);

            AddStep("release lctrl", () => InputManager.ReleaseKey(Key.LControl));
            AddAssert("release received", () => releasedReceived);
        }

        [Test]
        public void TestSingleKeyRepeatEvents()
        {
            int pressedReceived = 0;
            int repeatedReceived = 0;
            bool releasedReceived = false;

            AddStep("add container", () =>
            {
                pressedReceived = 0;
                repeatedReceived = 0;
                releasedReceived = false;

                Child = new TestKeyBindingContainer
                {
                    Child = new TestKeyBindingReceptor
                    {
                        Pressed = a => pressedReceived += a == TestAction.ActionA ? 1 : 0,
                        Repeated = a => repeatedReceived += a == TestAction.ActionA ? 1 : 0,
                        Released = a => releasedReceived = a == TestAction.ActionA
                    }
                };
            });

            AddStep("press A", () => InputManager.PressKey(Key.A));
            AddAssert("press received", () => pressedReceived == 1);

            for (int i = 0; i < 10; i++)
            {
                int localI = i + 1;
                AddUntilStep($"repeat #{1 + i} received", () => repeatedReceived >= localI);
            }

            AddStep("release A", () => InputManager.ReleaseKey(Key.A));
            AddAssert("release received", () => releasedReceived);

            AddAssert("only one press received", () => pressedReceived == 1);
        }

        [Test]
        public void TestKeyRepeatDoesntFireWhenNotAlive()
        {
            int pressedReceived = 0;
            int repeatedReceived = 0;
            bool releasedReceived = false;
            TestKeyBindingReceptor receptor = null;

            AddStep("add container", () =>
            {
                pressedReceived = 0;
                repeatedReceived = 0;
                releasedReceived = false;

                Child = new TestKeyBindingContainer
                {
                    Child = receptor = new TestKeyBindingReceptor
                    {
                        Pressed = a => pressedReceived += a == TestAction.ActionA ? 1 : 0,
                        Repeated = a => repeatedReceived += a == TestAction.ActionA ? 1 : 0,
                        Released = a => releasedReceived = a == TestAction.ActionA
                    }
                };
            });

            AddStep("press A", () => InputManager.PressKey(Key.A));
            AddUntilStep("wait for non-zero repeated", () => repeatedReceived > 0);

            AddStep("hide receptor", () => receptor.Hide());

            int stopReceivingCheck = 0;
            AddStep("store count", () => stopReceivingCheck = repeatedReceived);
            AddWaitStep("wait some", 5);
            AddAssert("ensure not incrementing", () => stopReceivingCheck == repeatedReceived);

            AddStep("release A", () => InputManager.ReleaseKey(Key.A));
            AddAssert("release received", () => releasedReceived);
            AddAssert("only one press received", () => pressedReceived == 1);
        }

        [Test]
        public void TestKeyCombinationRepeatEvents()
        {
            bool pressedReceived = false;
            bool repeatedReceived = false;
            bool releasedReceived = false;

            AddStep("add container", () =>
            {
                pressedReceived = false;
                repeatedReceived = false;
                releasedReceived = false;

                Child = new TestKeyBindingContainer
                {
                    Child = new TestKeyBindingReceptor
                    {
                        Pressed = a => pressedReceived = a == TestAction.ActionAB,
                        Repeated = a => repeatedReceived = a == TestAction.ActionAB,
                        Released = a => releasedReceived = a == TestAction.ActionAB,
                    }
                };
            });

            AddStep("press A+B", () =>
            {
                InputManager.PressKey(Key.A);
                InputManager.PressKey(Key.B);
            });
            AddAssert("press received", () => pressedReceived);

            for (int i = 0; i < 10; i++)
            {
                AddUntilStep($"repeat #{1 + i} received", () => repeatedReceived);
                AddStep("reset for next repeat", () => repeatedReceived = false);
            }

            AddStep("release A", () => InputManager.ReleaseKey(Key.A));
            AddAssert("release received", () => releasedReceived);

            AddStep("reset for potential repeat", () => repeatedReceived = false);
            AddWaitStep("wait", 5);
            AddAssert("no repeat received", () => !repeatedReceived);

            AddStep("release B", () => InputManager.ReleaseKey(Key.B));
        }

        private class TestKeyBindingReceptor : Drawable, IKeyBindingHandler<TestAction>
        {
            public Action<TestAction> Pressed;
            public Action<TestAction> Repeated;
            public Action<TestAction> Released;

            public TestKeyBindingReceptor()
            {
                RelativeSizeAxes = Axes.Both;
            }

            public bool OnPressed(KeyBindingPressEvent<TestAction> e)
            {
                if (e.Repeat)
                    Repeated?.Invoke(e.Action);
                else
                    Pressed?.Invoke(e.Action);

                return true;
            }

            public void OnReleased(KeyBindingReleaseEvent<TestAction> e)
            {
                Released?.Invoke(e.Action);
            }
        }

        private class TestKeyBindingContainer : KeyBindingContainer<TestAction>
        {
            public override IEnumerable<IKeyBinding> DefaultKeyBindings => new IKeyBinding[]
            {
                new KeyBinding(InputKey.A, TestAction.ActionA),
                new KeyBinding(new KeyCombination(InputKey.A, InputKey.B), TestAction.ActionAB),
                new KeyBinding(InputKey.Enter, TestAction.ActionEnter),
                new KeyBinding(InputKey.Control, TestAction.ActionControl)
            };
        }

        private enum TestAction
        {
            ActionA,
            ActionAB,
            ActionEnter,
            ActionControl
        }
    }
}
