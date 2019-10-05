// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Input;
using osu.Framework.Testing;
using osuTK;
using osuTK.Input;

namespace osu.Framework.Tests.Visual.Drawables
{
    public class TestSuiteFocus : ManualInputManagerTestSuite<TestSceneFocus>
    {
        public TestSuiteFocus()
        {
            RelativeSizeAxes = Axes.Both;
        }

        [SetUp]
        public override void SetUp() => Schedule(() =>
        {
            base.SetUp();

            Scene.SetUpScene();
        });

        [Test]
        public void FocusedOverlayTakesFocusOnShow()
        {
            AddAssert("overlay not visible", () => Scene.Overlay.State.Value == Visibility.Hidden);
            checkNotFocused(() => Scene.Overlay);

            AddStep("show overlay", () => Scene.Overlay.Show());
            checkFocused(() => Scene.Overlay);

            AddStep("hide overlay", () => Scene.Overlay.Hide());
            checkNotFocused(() => Scene.Overlay);
        }

        [Test]
        public void FocusedOverlayLosesFocusOnClickAway()
        {
            AddAssert("overlay not visible", () => Scene.Overlay.State.Value == Visibility.Hidden);
            checkNotFocused(() => Scene.Overlay);

            AddStep("show overlay", () => Scene.Overlay.Show());
            checkFocused(() => Scene.Overlay);

            AddStep("click away", () =>
            {
                InputManager.MoveMouseTo(Vector2.One);
                InputManager.Click(MouseButton.Left);
            });

            checkNotFocused(() => Scene.Overlay);
            checkFocused(() => Scene.RequestingFocus);
        }

        [Test]
        public void RequestsFocusKeepsFocusOnClickAway()
        {
            checkFocused(() => Scene.RequestingFocus);

            AddStep("click away", () =>
            {
                InputManager.MoveMouseTo(Vector2.One);
                InputManager.Click(MouseButton.Left);
            });

            checkFocused(() => Scene.RequestingFocus);
        }

        [Test]
        public void RequestsFocusLosesFocusOnClickingFocused()
        {
            checkFocused(() => Scene.RequestingFocus);

            AddStep("click top left", () =>
            {
                InputManager.MoveMouseTo(Scene.FocusTopLeft);
                InputManager.Click(MouseButton.Left);
            });

            checkFocused(() => Scene.FocusTopLeft);

            AddStep("click bottom right", () =>
            {
                InputManager.MoveMouseTo(Scene.FocusBottomRight);
                InputManager.Click(MouseButton.Left);
            });

            checkFocused(() => Scene.FocusBottomRight);
        }

        [Test]
        public void ShowOverlayInteractions()
        {
            AddStep("click bottom left", () =>
            {
                InputManager.MoveMouseTo(Scene.FocusBottomLeft);
                InputManager.Click(MouseButton.Left);
            });

            checkFocused(() => Scene.FocusBottomLeft);

            AddStep("show overlay", () => Scene.Overlay.Show());

            checkFocused(() => Scene.Overlay);
            checkNotFocused(() => Scene.FocusBottomLeft);

            // click is blocked by overlay so doesn't select bottom left first click
            AddStep("click", () => InputManager.Click(MouseButton.Left));
            checkFocused(() => Scene.RequestingFocus);

            // second click selects bottom left
            AddStep("click", () => InputManager.Click(MouseButton.Left));
            checkFocused(() => Scene.FocusBottomLeft);

            // further click has no effect
            AddStep("click", () => InputManager.Click(MouseButton.Left));
            checkFocused(() => Scene.FocusBottomLeft);
        }

        [Test]
        public void InputPropagation()
        {
            AddStep("Focus bottom left", () =>
            {
                InputManager.MoveMouseTo(Scene.FocusBottomLeft);
                InputManager.Click(MouseButton.Left);
            });
            AddStep("Press a key (blocking)", () =>
            {
                InputManager.PressKey(Key.A);
                InputManager.ReleaseKey(Key.A);
            });
            AddAssert("Received the key", () =>
                Scene.FocusBottomLeft.KeyDownCount == 1 && Scene.FocusBottomLeft.KeyUpCount == 1 &&
                Scene.FocusBottomRight.KeyDownCount == 0 && Scene.FocusBottomRight.KeyUpCount == 1);
            AddStep("Press a joystick (non blocking)", () =>
            {
                InputManager.PressJoystickButton(JoystickButton.Button1);
                InputManager.ReleaseJoystickButton(JoystickButton.Button1);
            });
            AddAssert("Received the joystick button", () =>
                Scene.FocusBottomLeft.JoystickPressCount == 1 && Scene.FocusBottomLeft.JoystickReleaseCount == 1 &&
                Scene.FocusBottomRight.JoystickPressCount == 1 && Scene.FocusBottomRight.JoystickReleaseCount == 1);
        }

        private void checkFocused(Func<Drawable> d) => AddAssert("check focus", () => d().HasFocus);
        private void checkNotFocused(Func<Drawable> d) => AddAssert("check not focus", () => !d().HasFocus);
    }
}
