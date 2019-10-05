// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Input;
using osu.Framework.Testing;
using osuTK.Input;

namespace osu.Framework.Tests.Visual.Input
{
    public class TestSuitePassThroughInputManager : ManualInputManagerTestSuite<TestScenePassThroughInputManager>
    {
        public TestSuitePassThroughInputManager()
        {
            RelativeSizeAxes = Axes.Both;
        }

        private void addTestInputManagerStep()
        {
            AddStep("Add InputManager", () => { Scene.AddInputManager(); });
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
            AddStep("Press mouse left", () => InputManager.PressButton(MouseButton.Left));
            AddStep("Press A", () => InputManager.PressKey(Key.A));
            AddStep("Press Joystick", () => InputManager.PressJoystickButton(JoystickButton.Button1));
            addTestInputManagerStep();
            AddAssert("mouse left not pressed", () => !Scene.Mouse.IsPressed(MouseButton.Left));
            AddAssert("A pressed", () => Scene.Keyboard.IsPressed(Key.A));
            AddAssert("Joystick pressed", () => Scene.Joystick.IsPressed(JoystickButton.Button1));
            AddStep("Release", () =>
            {
                InputManager.ReleaseButton(MouseButton.Left);
                InputManager.ReleaseKey(Key.A);
                InputManager.ReleaseJoystickButton(JoystickButton.Button1);
            });
            AddAssert("All released", () => !Scene.Mouse.HasAnyButtonPressed && !Scene.Keyboard.HasAnyButtonPressed && !Scene.Joystick.HasAnyButtonPressed);
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
            AddAssert("pressed", () => Scene.Mouse.IsPressed(MouseButton.Left) && Scene.Keyboard.IsPressed(Key.A) && Scene.Joystick.IsPressed(JoystickButton.Button1));
            AddStep("UseParentInput = false", () => Scene.TestSceneInputManager.UseParentInput = false);
            AddAssert("still pressed", () => Scene.Mouse.IsPressed(MouseButton.Left) && Scene.Keyboard.IsPressed(Key.A) && Scene.Joystick.IsPressed(JoystickButton.Button1));
            AddStep("Release on parent", () =>
            {
                InputManager.ReleaseButton(MouseButton.Left);
                InputManager.ReleaseKey(Key.A);
                InputManager.ReleaseJoystickButton(JoystickButton.Button1);
            });
            AddAssert("doen't affect child", () => Scene.Mouse.IsPressed(MouseButton.Left) && Scene.Keyboard.IsPressed(Key.A) && Scene.Joystick.IsPressed(JoystickButton.Button1));
            AddStep("UseParentInput = true", () => Scene.TestSceneInputManager.UseParentInput = true);
            AddAssert("all synced", () => !Scene.Mouse.IsPressed(MouseButton.Left) && !Scene.Keyboard.IsPressed(Key.A) && !Scene.Joystick.IsPressed(JoystickButton.Button1));
        }

        [Test]
        public void MouseDownNoSync()
        {
            addTestInputManagerStep();
            AddStep("UseParentInput = false", () => Scene.TestSceneInputManager.UseParentInput = false);
            AddStep("Press left", () => InputManager.PressButton(MouseButton.Left));
            AddStep("UseParentInput = true", () => Scene.TestSceneInputManager.UseParentInput = true);
            AddAssert("not pressed", () => !Scene.Mouse.IsPressed(MouseButton.Left));
        }

        [Test]
        public void NoMouseUp()
        {
            addTestInputManagerStep();
            AddStep("Press left", () => InputManager.PressButton(MouseButton.Left));
            AddStep("UseParentInput = false", () => Scene.TestSceneInputManager.UseParentInput = false);
            AddStep("Release and press", () =>
            {
                InputManager.ReleaseButton(MouseButton.Left);
                InputManager.PressButton(MouseButton.Left);
            });
            AddStep("UseParentInput = true", () => Scene.TestSceneInputManager.UseParentInput = true);
            AddAssert("pressed", () => Scene.Mouse.IsPressed(MouseButton.Left));
            AddAssert("mouse up count == 0", () => Scene.TestSceneInputManager.Status.MouseUpCount == 0);
        }
    }
}
