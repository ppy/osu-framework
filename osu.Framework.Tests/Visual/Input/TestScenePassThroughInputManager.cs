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
            Steps.AddStep("Add InputManager", () =>
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
        public override void SetUp() => Schedule(() =>
        {
            base.SetUp();
            ChildrenEnumerable = Enumerable.Empty<Drawable>();
        });

        [Test]
        public void ReceiveInitialState()
        {
            Steps.AddStep("Press mouse left", () => InputManager.PressButton(MouseButton.Left));
            Steps.AddStep("Press A", () => InputManager.PressKey(Key.A));
            Steps.AddStep("Press Joystick", () => InputManager.PressJoystickButton(JoystickButton.Button1));
            addTestInputManagerStep();
            Steps.AddAssert("mouse left not pressed", () => !mouse.IsPressed(MouseButton.Left));
            Steps.AddAssert("A pressed", () => keyboard.IsPressed(Key.A));
            Steps.AddAssert("Joystick pressed", () => joystick.IsPressed(JoystickButton.Button1));
            Steps.AddStep("Release", () =>
            {
                InputManager.ReleaseButton(MouseButton.Left);
                InputManager.ReleaseKey(Key.A);
                InputManager.ReleaseJoystickButton(JoystickButton.Button1);
            });
            Steps.AddAssert("All released", () => !mouse.HasAnyButtonPressed && !keyboard.HasAnyButtonPressed && !joystick.HasAnyButtonPressed);
        }

        [Test]
        public void UseParentInputChange()
        {
            addTestInputManagerStep();
            Steps.AddStep("Press buttons", () =>
            {
                InputManager.PressButton(MouseButton.Left);
                InputManager.PressKey(Key.A);
                InputManager.PressJoystickButton(JoystickButton.Button1);
            });
            Steps.AddAssert("pressed", () => mouse.IsPressed(MouseButton.Left) && keyboard.IsPressed(Key.A) && joystick.IsPressed(JoystickButton.Button1));
            Steps.AddStep("UseParentInput = false", () => testInputManager.UseParentInput = false);
            Steps.AddAssert("still pressed", () => mouse.IsPressed(MouseButton.Left) && keyboard.IsPressed(Key.A) && joystick.IsPressed(JoystickButton.Button1));
            Steps.AddStep("Release on parent", () =>
            {
                InputManager.ReleaseButton(MouseButton.Left);
                InputManager.ReleaseKey(Key.A);
                InputManager.ReleaseJoystickButton(JoystickButton.Button1);
            });
            Steps.AddAssert("doen't affect child", () => mouse.IsPressed(MouseButton.Left) && keyboard.IsPressed(Key.A) && joystick.IsPressed(JoystickButton.Button1));
            Steps.AddStep("UseParentInput = true", () => testInputManager.UseParentInput = true);
            Steps.AddAssert("all synced", () => !mouse.IsPressed(MouseButton.Left) && !keyboard.IsPressed(Key.A) && !joystick.IsPressed(JoystickButton.Button1));
        }

        [Test]
        public void MouseDownNoSync()
        {
            addTestInputManagerStep();
            Steps.AddStep("UseParentInput = false", () => testInputManager.UseParentInput = false);
            Steps.AddStep("Press left", () => InputManager.PressButton(MouseButton.Left));
            Steps.AddStep("UseParentInput = true", () => testInputManager.UseParentInput = true);
            Steps.AddAssert("not pressed", () => !mouse.IsPressed(MouseButton.Left));
        }

        [Test]
        public void NoMouseUp()
        {
            addTestInputManagerStep();
            Steps.AddStep("Press left", () => InputManager.PressButton(MouseButton.Left));
            Steps.AddStep("UseParentInput = false", () => testInputManager.UseParentInput = false);
            Steps.AddStep("Release and press", () =>
            {
                InputManager.ReleaseButton(MouseButton.Left);
                InputManager.PressButton(MouseButton.Left);
            });
            Steps.AddStep("UseParentInput = true", () => testInputManager.UseParentInput = true);
            Steps.AddAssert("pressed", () => mouse.IsPressed(MouseButton.Left));
            Steps.AddAssert("mouse up count == 0", () => testInputManager.Status.MouseUpCount == 0);
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
