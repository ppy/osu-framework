// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Input;
using osu.Framework.Input.States;
using osu.Framework.Testing;
using osu.Framework.Testing.Input;
using osuTK;
using osuTK.Input;

namespace osu.Framework.Tests.Visual.Input
{
    public class TestScenePassThroughInputManager : ManualInputManagerTestScene
    {
        public TestScenePassThroughInputManager()
        {
            RelativeSizeAxes = Axes.Both;
        }

        private TestInputManager testInputManager;
        private InputState state;
        private ButtonStates<MouseButton> mouse;
        private ButtonStates<Key> keyboard;
        private ButtonStates<JoystickButton> joystick;

        private void addTestInputManagerStep()
        {
            AddStep("Add InputManager", () =>
            {
                testInputManager = new TestInputManager();
                Add(testInputManager);
                state = testInputManager.CurrentState;
                mouse = state.Mouse.Buttons;
                keyboard = state.Keyboard.Keys;
                joystick = state.Joystick.Buttons;
            });
        }

        [SetUp]
        public new void SetUp() => Schedule(() =>
        {
            ChildrenEnumerable = Enumerable.Empty<Drawable>();
        });

        [Test]
        public void ReceiveInitialState()
        {
            AddStep("Press mouse left", () => InputManager.PressButton(MouseButton.Left));
            AddStep("Press A", () => InputManager.PressKey(Key.A));
            AddStep("Press Joystick", () => InputManager.PressJoystickButton(JoystickButton.Button1));
            addTestInputManagerStep();
            AddAssert("mouse left not pressed", () => !mouse.IsPressed(MouseButton.Left));
            AddAssert("A pressed", () => keyboard.IsPressed(Key.A));
            AddAssert("Joystick pressed", () => joystick.IsPressed(JoystickButton.Button1));
            AddStep("Release", () =>
            {
                InputManager.ReleaseButton(MouseButton.Left);
                InputManager.ReleaseKey(Key.A);
                InputManager.ReleaseJoystickButton(JoystickButton.Button1);
            });
            AddAssert("All released", () => !mouse.HasAnyButtonPressed && !keyboard.HasAnyButtonPressed && !joystick.HasAnyButtonPressed);
        }

        [Test]
        public void UseParentInputChange()
        {
            addTestInputManagerStep();
            AddStep("Press buttons", () =>
            {
                InputManager.PressButton(MouseButton.Left);
                InputManager.PressKey(Key.A);
                InputManager.PressJoystickButton(JoystickButton.Button1);
            });
            AddAssert("pressed", () => mouse.IsPressed(MouseButton.Left) && keyboard.IsPressed(Key.A) && joystick.IsPressed(JoystickButton.Button1));
            AddStep("UseParentInput = false", () => testInputManager.UseParentInput = false);
            AddAssert("still pressed", () => mouse.IsPressed(MouseButton.Left) && keyboard.IsPressed(Key.A) && joystick.IsPressed(JoystickButton.Button1));
            AddStep("Release on parent", () =>
            {
                InputManager.ReleaseButton(MouseButton.Left);
                InputManager.ReleaseKey(Key.A);
                InputManager.ReleaseJoystickButton(JoystickButton.Button1);
            });
            AddAssert("doen't affect child", () => mouse.IsPressed(MouseButton.Left) && keyboard.IsPressed(Key.A) && joystick.IsPressed(JoystickButton.Button1));
            AddStep("UseParentInput = true", () => testInputManager.UseParentInput = true);
            AddAssert("all synced", () => !mouse.IsPressed(MouseButton.Left) && !keyboard.IsPressed(Key.A) && !joystick.IsPressed(JoystickButton.Button1));
        }

        [Test]
        public void TestUpReceivedOnDownFromSync()
        {
            addTestInputManagerStep();
            AddStep("UseParentInput = false", () => testInputManager.UseParentInput = false);
            AddStep("press keyboard", () => InputManager.PressKey(Key.A));
            AddAssert("key not pressed", () => !testInputManager.CurrentState.Keyboard.Keys.HasAnyButtonPressed);

            AddStep("UseParentInput = true", () => testInputManager.UseParentInput = true);
            AddAssert("key pressed", () => testInputManager.CurrentState.Keyboard.Keys.Single() == Key.A);

            AddStep("release keyboard", () => InputManager.ReleaseKey(Key.A));
            AddAssert("key released", () => !testInputManager.CurrentState.Keyboard.Keys.HasAnyButtonPressed);
        }

        [Test]
        public void MouseDownNoSync()
        {
            addTestInputManagerStep();
            AddStep("UseParentInput = false", () => testInputManager.UseParentInput = false);
            AddStep("Press left", () => InputManager.PressButton(MouseButton.Left));
            AddStep("UseParentInput = true", () => testInputManager.UseParentInput = true);
            AddAssert("not pressed", () => !mouse.IsPressed(MouseButton.Left));
        }

        [Test]
        public void NoMouseUp()
        {
            addTestInputManagerStep();
            AddStep("Press left", () => InputManager.PressButton(MouseButton.Left));
            AddStep("UseParentInput = false", () => testInputManager.UseParentInput = false);
            AddStep("Release and press", () =>
            {
                InputManager.ReleaseButton(MouseButton.Left);
                InputManager.PressButton(MouseButton.Left);
            });
            AddStep("UseParentInput = true", () => testInputManager.UseParentInput = true);
            AddAssert("pressed", () => mouse.IsPressed(MouseButton.Left));
            AddAssert("mouse up count == 0", () => testInputManager.Status.MouseUpCount == 0);
        }

        [Test]
        public void TestTouchInput()
        {
            addTestInputManagerStep();
            AddStep("begin first touch", () => InputManager.BeginTouch(new Touch(TouchSource.Touch1, Vector2.Zero)));
            AddAssert("synced properly", () =>
                testInputManager.CurrentState.Touch.ActiveSources.Single() == TouchSource.Touch1 &&
                testInputManager.CurrentState.Touch.TouchPositions[(int)TouchSource.Touch1] == Vector2.Zero);

            AddStep("UseParentInput = false", () => testInputManager.UseParentInput = false);
            AddStep("end first touch", () => InputManager.EndTouch(new Touch(TouchSource.Touch1, Vector2.Zero)));
            AddStep("begin second touch", () => InputManager.BeginTouch(new Touch(TouchSource.Touch2, Vector2.One)));

            AddStep("UseParentInput = true", () => testInputManager.UseParentInput = true);
            AddAssert("synced properly", () =>
                testInputManager.CurrentState.Touch.ActiveSources.Single() == TouchSource.Touch2 &&
                testInputManager.CurrentState.Touch.TouchPositions[(int)TouchSource.Touch2] == Vector2.One);
        }

        public class TestInputManager : ManualInputManager
        {
            public readonly TestSceneInputManager.ContainingInputManagerStatusText Status;

            public TestInputManager()
            {
                Size = new Vector2(0.8f);
                Origin = Anchor.Centre;
                Anchor = Anchor.Centre;
                Child = Status = new TestSceneInputManager.ContainingInputManagerStatusText();
            }
        }
    }
}
