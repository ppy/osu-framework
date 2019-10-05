// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Input;
using osu.Framework.Input.States;
using osu.Framework.Testing;
using osu.Framework.Testing.Input;
using osuTK;
using osuTK.Input;

namespace osu.Framework.Tests.Visual.Input
{
    public class TestScenePassThroughInputManager : TestScene
    {
        public TestInputManager TestSceneInputManager;
        public InputState State;
        public ButtonStates<MouseButton> Mouse;
        public ButtonStates<Key> Keyboard;
        public ButtonStates<JoystickButton> Joystick;

        public void AddInputManager()
        {
            TestSceneInputManager = new TestInputManager();
            Add(TestSceneInputManager);
            State = TestSceneInputManager.CurrentState;
            Mouse = State.Mouse.Buttons;
            Keyboard = State.Keyboard.Keys;
            Joystick = State.Joystick.Buttons;
        }

        public class TestInputManager : ManualInputManager
        {
            public readonly TestSuiteInputManager.ContainingInputManagerStatusText Status;

            public TestInputManager()
            {
                Size = new Vector2(0.8f);
                Origin = Anchor.Centre;
                Anchor = Anchor.Centre;
                Child = Status = new TestSuiteInputManager.ContainingInputManagerStatusText();
            }
        }
    }
}
