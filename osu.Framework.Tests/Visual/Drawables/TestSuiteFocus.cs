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

            TestScene.SetUpScene();
        });

        [Test]
        public void FocusedOverlayTakesFocusOnShow()
        {
            AddAssert("overlay not visible", () => TestScene.Overlay.State.Value == Visibility.Hidden);
            checkNotFocused(() => TestScene.Overlay);

            AddStep("show overlay", () => TestScene.Overlay.Show());
            checkFocused(() => TestScene.Overlay);

            AddStep("hide overlay", () => TestScene.Overlay.Hide());
            checkNotFocused(() => TestScene.Overlay);
        }

        [Test]
        public void FocusedOverlayLosesFocusOnClickAway()
        {
            AddAssert("overlay not visible", () => TestScene.Overlay.State.Value == Visibility.Hidden);
            checkNotFocused(() => TestScene.Overlay);

            AddStep("show overlay", () => TestScene.Overlay.Show());
            checkFocused(() => TestScene.Overlay);

            AddStep("click away", () =>
            {
                InputManager.MoveMouseTo(Vector2.One);
                InputManager.Click(MouseButton.Left);
            });

            checkNotFocused(() => TestScene.Overlay);
            checkFocused(() => TestScene.RequestingFocus);
        }

        [Test]
        public void RequestsFocusKeepsFocusOnClickAway()
        {
            checkFocused(() => TestScene.RequestingFocus);

            AddStep("click away", () =>
            {
                InputManager.MoveMouseTo(Vector2.One);
                InputManager.Click(MouseButton.Left);
            });

            checkFocused(() => TestScene.RequestingFocus);
        }

        [Test]
        public void RequestsFocusLosesFocusOnClickingFocused()
        {
            checkFocused(() => TestScene.RequestingFocus);

            AddStep("click top left", () =>
            {
                InputManager.MoveMouseTo(TestScene.FocusTopLeft);
                InputManager.Click(MouseButton.Left);
            });

            checkFocused(() => TestScene.FocusTopLeft);

            AddStep("click bottom right", () =>
            {
                InputManager.MoveMouseTo(TestScene.FocusBottomRight);
                InputManager.Click(MouseButton.Left);
            });

            checkFocused(() => TestScene.FocusBottomRight);
        }

        [Test]
        public void ShowOverlayInteractions()
        {
            AddStep("click bottom left", () =>
            {
                InputManager.MoveMouseTo(TestScene.FocusBottomLeft);
                InputManager.Click(MouseButton.Left);
            });

            checkFocused(() => TestScene.FocusBottomLeft);

            AddStep("show overlay", () => TestScene.Overlay.Show());

            checkFocused(() => TestScene.Overlay);
            checkNotFocused(() => TestScene.FocusBottomLeft);

            // click is blocked by overlay so doesn't select bottom left first click
            AddStep("click", () => InputManager.Click(MouseButton.Left));
            checkFocused(() => TestScene.RequestingFocus);

            // second click selects bottom left
            AddStep("click", () => InputManager.Click(MouseButton.Left));
            checkFocused(() => TestScene.FocusBottomLeft);

            // further click has no effect
            AddStep("click", () => InputManager.Click(MouseButton.Left));
            checkFocused(() => TestScene.FocusBottomLeft);
        }

        [Test]
        public void InputPropagation()
        {
            AddStep("Focus bottom left", () =>
            {
                InputManager.MoveMouseTo(TestScene.FocusBottomLeft);
                InputManager.Click(MouseButton.Left);
            });
            AddStep("Press a key (blocking)", () =>
            {
                InputManager.PressKey(Key.A);
                InputManager.ReleaseKey(Key.A);
            });
            AddAssert("Received the key", () =>
                TestScene.FocusBottomLeft.KeyDownCount == 1 && TestScene.FocusBottomLeft.KeyUpCount == 1 &&
                TestScene.FocusBottomRight.KeyDownCount == 0 && TestScene.FocusBottomRight.KeyUpCount == 1);
            AddStep("Press a joystick (non blocking)", () =>
            {
                InputManager.PressJoystickButton(JoystickButton.Button1);
                InputManager.ReleaseJoystickButton(JoystickButton.Button1);
            });
            AddAssert("Received the joystick button", () =>
                TestScene.FocusBottomLeft.JoystickPressCount == 1 && TestScene.FocusBottomLeft.JoystickReleaseCount == 1 &&
                TestScene.FocusBottomRight.JoystickPressCount == 1 && TestScene.FocusBottomRight.JoystickReleaseCount == 1);
        }

        private void checkFocused(Func<Drawable> d) => AddAssert("check focus", () => d().HasFocus);
        private void checkNotFocused(Func<Drawable> d) => AddAssert("check not focus", () => !d().HasFocus);
    }
}
