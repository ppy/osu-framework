// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

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

        /// <summary>
        /// Tests that a drawable that is removed from the hierarchy (or is otherwise removed from the input queues) won't receive OnKeyDown() events for every subsequent repeat.
        /// </summary>
        [Test]
        public void TestNoLongerValidDrawableDoesNotReceiveRepeat()
        {
            var receptors = new InputReceptor[2];

            AddStep("create hierarchy", () =>
            {
                Children = new Drawable[]
                {
                    receptors[0] = new InputReceptor { Size = new Vector2(100) },
                    receptors[1] = new InputReceptor { Size = new Vector2(100) }
                };
            });

            AddStep("press key", () => InputManager.PressKey(Key.A));
            AddUntilStep("wait for repeat on receptor 0", () => receptors[0].RepeatReceived);

            AddStep("remove receptor 0 & reset repeat", () =>
            {
                Remove(receptors[0]);
                receptors[0].RepeatReceived = false;
                receptors[1].RepeatReceived = false;
            });

            AddUntilStep("wait for repeat on receptor 1", () => receptors[1].RepeatReceived);
            AddAssert("receptor 0 did not receive repeat", () => !receptors[0].RepeatReceived);
        }

        /// <summary>
        /// Tests that a drawable that was previously removed from the hierarchy receives repeat OnKeyDown() events when re-added to the hierarchy,
        /// if it previously received a non-repeat OnKeyDown() event.
        /// </summary>
        [Test]
        public void TestReValidatedDrawableReceivesRepeat()
        {
            var receptors = new InputReceptor[2];

            AddStep("create hierarchy", () =>
            {
                Children = new Drawable[]
                {
                    receptors[0] = new InputReceptor { Size = new Vector2(100) },
                    receptors[1] = new InputReceptor { Size = new Vector2(100) }
                };
            });

            AddStep("press key", () => InputManager.PressKey(Key.A));
            AddStep("remove receptor 0 & reset repeat", () =>
            {
                Remove(receptors[0]);
                receptors[0].RepeatReceived = false;
                receptors[1].RepeatReceived = false;
            });

            AddUntilStep("wait for repeat on receptor 1", () => receptors[1].RepeatReceived);
            AddStep("add back receptor 0", () => Add(receptors[0]));

            AddUntilStep("wait for repeat on receptor 0", () => receptors[0].RepeatReceived);
        }

        private class InputReceptor : Box
        {
            public bool DownReceived { get; set; }
            public bool UpReceived { get; set; }
            public bool RepeatReceived { get; set; }

            public Func<bool> KeyDown;
            public Func<bool> KeyUp;

            protected override bool OnKeyDown(KeyDownEvent e)
            {
                if (e.Repeat)
                {
                    RepeatReceived = true;
                    return false;
                }

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
