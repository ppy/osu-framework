// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Input;
using osu.Framework.Testing;
using osuTK;
using osuTK.Input;

namespace osu.Framework.Tests.Visual.Testing
{
    public class TestSceneManualInputManagerTestScene : ManualInputManagerTestScene
    {
        protected override Vector2 InitialMousePosition => new Vector2(10f);

        [Test]
        public void TestResetInput()
        {
            AddStep("move mouse", () => InputManager.MoveMouseTo(Vector2.Zero));
            AddStep("press mouse", () => InputManager.PressButton(MouseButton.Left));
            AddStep("press key", () => InputManager.PressKey(Key.Z));
            AddStep("press joystick", () => InputManager.PressJoystickButton(JoystickButton.Button1));

            AddStep("reset input", ResetInput);

            AddAssert("mouse position reset", () => InputManager.CurrentState.Mouse.Position == InitialMousePosition);
            AddAssert("all input states released", () =>
                !InputManager.CurrentState.Mouse.Buttons.HasAnyButtonPressed &&
                !InputManager.CurrentState.Keyboard.Keys.HasAnyButtonPressed &&
                !InputManager.CurrentState.Joystick.Buttons.HasAnyButtonPressed);
        }

        /// <summary>
        /// Added to allow manual testing of the test cursor visuals.
        /// </summary>
        [Test]
        public void TestHoldLeftFromMaskedPosition()
        {
            AddStep("move mouse to screen zero", () => InputManager.MoveMouseTo(Vector2.Zero));
            AddStep("hold mouse left", () => InputManager.PressButton(MouseButton.Left));
            AddStep("move mouse to content center", () => InputManager.MoveMouseTo(Content.ScreenSpaceDrawQuad.Centre));
        }

        [Test]
        public void TestMousePositionSetToInitial() => AddAssert("mouse position set to initial", () => InputManager.CurrentState.Mouse.Position == InitialMousePosition);
    }
}
