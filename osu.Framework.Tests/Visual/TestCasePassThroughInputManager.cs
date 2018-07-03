// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Input;
using osu.Framework.Testing;
using osu.Framework.Testing.Input;
using OpenTK;
using OpenTK.Input;

namespace osu.Framework.Tests.Visual
{
    public class TestCasePassThroughInputManager : ManualInputManagerTestCase
    {
        public TestCasePassThroughInputManager()
        {
            RelativeSizeAxes = Axes.Both;
        }

        private TestInputManager testInputManager;
        private InputState state;
        private IMouseState mouse;
        private IKeyboardState keyboard;
        private IJoystickState joystick;
        private void addTestInputManagerStep()
        {
            AddStep("Add InputManager", () =>
            {
                testInputManager = new TestInputManager();
                Add(testInputManager);
                state = testInputManager.CurrentState;
                mouse = state.Mouse;
                keyboard = state.Keyboard;
                joystick = state.Joystick;
            });
        }

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            ChildrenEnumerable = Enumerable.Empty<Drawable>();
        }

        [Test]
        public void ReceiveInitialState()
        {
            AddStep("Press mouse left", () => InputManager.PressButton(MouseButton.Left));
            AddStep("Press A", () => InputManager.PressKey(Key.A));
            AddStep("Press Joystick", () => InputManager.PressJoystickButton(JoystickButton.Button1));
            addTestInputManagerStep();
            AddAssert("mouse left not pressed", () => !mouse.IsPressed(MouseButton.Left));
            AddAssert("A pressed", () => keyboard.IsPressed(Key.A));
            AddAssert("Joystick pressed", () => joystick.Buttons.IsPressed(JoystickButton.Button1));
            AddStep("Release", () =>
            {
                InputManager.ReleaseButton(MouseButton.Left);
                InputManager.ReleaseKey(Key.A);
                InputManager.ReleaseJoystickButton(JoystickButton.Button1);
            });
            AddAssert("All released", () => !mouse.HasAnyButtonPressed && !keyboard.Keys.HasAnyButtonPressed && !joystick.Buttons.HasAnyButtonPressed);
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
            AddAssert("pressed", () => mouse.IsPressed(MouseButton.Left) && keyboard.IsPressed(Key.A) && joystick.Buttons.IsPressed(JoystickButton.Button1));
            AddStep("UseParentInput = false", () => testInputManager.UseParentInput = false);
            AddAssert("pressed", () => !mouse.IsPressed(MouseButton.Left) && !keyboard.IsPressed(Key.A) && !joystick.Buttons.IsPressed(JoystickButton.Button1));
            AddStep("UseParentInput = true", () => testInputManager.UseParentInput = true);
            AddAssert("mouse not pressed", () => !mouse.IsPressed(MouseButton.Left));
            AddAssert("others pressed", () => keyboard.IsPressed(Key.A) && joystick.Buttons.IsPressed(JoystickButton.Button1));
        }

        public class TestInputManager : ManualInputManager
        {
            public TestInputManager()
            {
                Size = new Vector2(0.8f);
                Origin = Anchor.Centre;
                Anchor = Anchor.Centre;
                Child = new TestCaseInputManager.ContainingInputManagerStatusText();
            }
        }
    }
}
