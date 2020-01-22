// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
using osu.Framework.Testing;
using osuTK;
using osuTK.Input;

namespace osu.Framework.Tests.Input
{
    [HeadlessTest]
    public class KeyboardInputTest : ManualInputManagerTestScene
    {
        /// <summary>
        /// Tests that if the hierarchy is changed while a key is held, the <see cref="Drawable.OnKeyUp"/> event is
        /// only propagated to the hierarchy that originally handled <see cref="Drawable.OnKeyDown"/>.
        /// </summary>
        [Test]
        public void TestKeyUpOnlyPropagatedToOriginalTargets()
        {
            var receptors = new InputReceptor[3];

            AddStep("create hierarchy", () =>
            {
                Children = new Drawable[]
                {
                    receptors[0] = new InputReceptor
                    {
                        Size = new Vector2(100),
                        KeyDown = () => true
                    },
                    receptors[1] = new InputReceptor { Size = new Vector2(100) }
                };
            });

            AddStep("press key", () => InputManager.PressKey(Key.A));
            AddStep("add receptor above", () =>
            {
                Add(receptors[2] = new InputReceptor
                {
                    Size = new Vector2(100),
                    KeyDown = () => true,
                    KeyUp = () => true
                });
            });

            AddStep("release key", () => InputManager.ReleaseKey(Key.A));

            AddAssert("receptor 0 handled key down", () => receptors[0].DownReceived);
            AddAssert("receptor 0 handled key up", () => receptors[0].UpReceived);
            AddAssert("receptor 1 handled key down", () => receptors[1].DownReceived);
            AddAssert("receptor 1 handled key up", () => receptors[1].UpReceived);
            AddAssert("receptor 2 did not handle key down", () => !receptors[2].DownReceived);
            AddAssert("receptor 2 did not handle key up", () => !receptors[2].UpReceived);
        }

        private class InputReceptor : Box
        {
            public bool DownReceived { get; private set; }
            public bool UpReceived { get; private set; }

            public Func<bool> KeyDown;
            public Func<bool> KeyUp;

            protected override bool OnKeyDown(KeyDownEvent e)
            {
                if (e.Repeat)
                    return false;

                DownReceived = true;
                return KeyDown?.Invoke() ?? false;
            }

            protected override void OnKeyUp(KeyUpEvent e)
            {
                UpReceived = true;
                KeyUp?.Invoke();
            }
        }
    }
}
