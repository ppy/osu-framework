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
            AddStep("Add InputManager", () => { TestScene.AddInputManager(); });
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
            AddAssert("mouse left not pressed", () => !TestScene.Mouse.IsPressed(MouseButton.Left));
            AddAssert("A pressed", () => TestScene.Keyboard.IsPressed(Key.A));
            AddAssert("Joystick pressed", () => TestScene.Joystick.IsPressed(JoystickButton.Button1));
            AddStep("Release", () =>
            {
                InputManager.ReleaseButton(MouseButton.Left);
                InputManager.ReleaseKey(Key.A);
                InputManager.ReleaseJoystickButton(JoystickButton.Button1);
            });
            AddAssert("All released", () => !TestScene.Mouse.HasAnyButtonPressed && !TestScene.Keyboard.HasAnyButtonPressed && !TestScene.Joystick.HasAnyButtonPressed);
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
            AddAssert("pressed", () => TestScene.Mouse.IsPressed(MouseButton.Left) && TestScene.Keyboard.IsPressed(Key.A) && TestScene.Joystick.IsPressed(JoystickButton.Button1));
            AddStep("UseParentInput = false", () => TestScene.TestSceneInputManager.UseParentInput = false);
            AddAssert("still pressed", () => TestScene.Mouse.IsPressed(MouseButton.Left) && TestScene.Keyboard.IsPressed(Key.A) && TestScene.Joystick.IsPressed(JoystickButton.Button1));
            AddStep("Release on parent", () =>
            {
                InputManager.ReleaseButton(MouseButton.Left);
                InputManager.ReleaseKey(Key.A);
                InputManager.ReleaseJoystickButton(JoystickButton.Button1);
            });
            AddAssert("doen't affect child", () => TestScene.Mouse.IsPressed(MouseButton.Left) && TestScene.Keyboard.IsPressed(Key.A) && TestScene.Joystick.IsPressed(JoystickButton.Button1));
            AddStep("UseParentInput = true", () => TestScene.TestSceneInputManager.UseParentInput = true);
            AddAssert("all synced", () => !TestScene.Mouse.IsPressed(MouseButton.Left) && !TestScene.Keyboard.IsPressed(Key.A) && !TestScene.Joystick.IsPressed(JoystickButton.Button1));
        }

        [Test]
        public void MouseDownNoSync()
        {
            addTestInputManagerStep();
            AddStep("UseParentInput = false", () => TestScene.TestSceneInputManager.UseParentInput = false);
            AddStep("Press left", () => InputManager.PressButton(MouseButton.Left));
            AddStep("UseParentInput = true", () => TestScene.TestSceneInputManager.UseParentInput = true);
            AddAssert("not pressed", () => !TestScene.Mouse.IsPressed(MouseButton.Left));
        }

        [Test]
        public void NoMouseUp()
        {
            addTestInputManagerStep();
            AddStep("Press left", () => InputManager.PressButton(MouseButton.Left));
            AddStep("UseParentInput = false", () => TestScene.TestSceneInputManager.UseParentInput = false);
            AddStep("Release and press", () =>
            {
                InputManager.ReleaseButton(MouseButton.Left);
                InputManager.PressButton(MouseButton.Left);
            });
            AddStep("UseParentInput = true", () => TestScene.TestSceneInputManager.UseParentInput = true);
            AddAssert("pressed", () => TestScene.Mouse.IsPressed(MouseButton.Left));
            AddAssert("mouse up count == 0", () => TestScene.TestSceneInputManager.Status.MouseUpCount == 0);
        }
    }
}
