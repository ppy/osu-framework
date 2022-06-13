// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input;
using osu.Framework.Input.Events;
using osu.Framework.Testing;
using osuTK;

namespace osu.Framework.Tests.Input
{
    [HeadlessTest]
    public class JoystickInputTest : ManualInputManagerTestScene
    {
        /// <summary>
        /// Tests that if the hierarchy is changed while a joystick button is held, the <see cref="Drawable.OnJoystickRelease"/> event is
        /// only propagated to the hierarchy that originally handled <see cref="Drawable.OnJoystickPress"/>.
        /// </summary>
        [Test]
        public void TestJoystickReleaseOnlyPropagatedToOriginalTargets()
        {
            var receptors = new InputReceptor[3];

            AddStep("create hierarchy", () =>
            {
                Children = new Drawable[]
                {
                    receptors[0] = new InputReceptor
                    {
                        Size = new Vector2(100),
                        Press = () => true
                    },
                    receptors[1] = new InputReceptor { Size = new Vector2(100) }
                };
            });

            AddStep("press a button", () => InputManager.PressJoystickButton(JoystickButton.Button1));
            AddStep("add receptor above", () =>
            {
                Add(receptors[2] = new InputReceptor
                {
                    Size = new Vector2(100),
                    Press = () => true,
                    Release = () => true
                });
            });

            AddStep("release key", () => InputManager.ReleaseJoystickButton(JoystickButton.Button1));

            AddAssert("receptor 0 handled key down", () => receptors[0].PressReceived);
            AddAssert("receptor 0 handled key up", () => receptors[0].ReleaseReceived);
            AddAssert("receptor 1 handled key down", () => receptors[1].PressReceived);
            AddAssert("receptor 1 handled key up", () => receptors[1].ReleaseReceived);
            AddAssert("receptor 2 did not handle key down", () => !receptors[2].PressReceived);
            AddAssert("receptor 2 did not handle key up", () => !receptors[2].ReleaseReceived);
        }

        private class InputReceptor : Box
        {
            public bool PressReceived { get; private set; }
            public bool ReleaseReceived { get; private set; }

            public Func<bool> Press;
            public Func<bool> Release;

            protected override bool OnJoystickPress(JoystickPressEvent e)
            {
                PressReceived = true;
                return Press?.Invoke() ?? false;
            }

            protected override void OnJoystickRelease(JoystickReleaseEvent e)
            {
                ReleaseReceived = true;
                Release?.Invoke();
            }
        }
    }
}
