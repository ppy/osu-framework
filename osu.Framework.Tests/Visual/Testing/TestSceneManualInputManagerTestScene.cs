// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input;
using osu.Framework.Testing;
using osu.Framework.Testing.Input;
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

        [Test]
        public void TestPlatformAction()
        {
            BasicTextBox textbox = null;

            AddStep("add textbox", () => Child = textbox = new BasicTextBox
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Size = new Vector2(100, 20),
                Text = "test text"
            });

            AddStep("focus textbox", () =>
            {
                InputManager.MoveMouseTo(textbox);
                InputManager.Click(MouseButton.Left);
            });

            AddAssert("text is not selected", () => string.IsNullOrEmpty(textbox.SelectedText));

            AddStep("press platform action", () => InputManager.Keys(PlatformAction.SelectAll));

            AddAssert("text is selected", () => textbox.SelectedText == "test text");
        }

        [Test]
        public void TestHoldLeftFromMaskedPosition()
        {
            TestCursor cursor = null;

            AddStep("retrieve cursor", () => cursor = (TestCursor)InputManager.ChildrenOfType<TestCursorContainer>().Single().ActiveCursor);

            AddStep("move mouse to screen zero", () => InputManager.MoveMouseTo(Vector2.Zero));
            AddAssert("ensure cursor masked away", () => cursor.IsMaskedAway);

            AddStep("hold left button", () => InputManager.PressButton(MouseButton.Left));
            AddAssert("cursor updated to hold", () => cursor.Left.IsPresent);

            AddStep("move mouse to content", () => InputManager.MoveMouseTo(Content));
            AddAssert("cursor still holding", () => cursor.Left.IsPresent);
        }

        [Test]
        public void TestMousePositionSetToInitial() => AddAssert("mouse position set to initial", () => InputManager.CurrentState.Mouse.Position == InitialMousePosition);
    }
}
