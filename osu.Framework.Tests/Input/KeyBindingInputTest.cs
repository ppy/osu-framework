// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Collections.Generic;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Framework.Testing;
using osuTK;
using osuTK.Input;

namespace osu.Framework.Tests.Input
{
    [HeadlessTest]
    public class KeyBindingInputTest : ManualInputManagerTestScene
    {
        /// <summary>
        /// Tests that if the current input queue is changed, drawables that originally handled <see cref="IKeyBindingHandler{T}.OnPressed"/>
        /// will receive a corresponding <see cref="IKeyBindingHandler{T}.OnReleased"/> event.
        /// </summary>
        [Test]
        public void TestReleaseAlwaysPressedToOriginalTargets()
        {
            InputReceptor receptorBelow = null;
            InputReceptor receptorAbove = null;

            AddStep("setup", () =>
            {
                Child = new TestKeyBindingContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Children = new Drawable[]
                    {
                        receptorBelow = new InputReceptor(true)
                        {
                            Size = new Vector2(100),
                        },
                        receptorAbove = new InputReceptor(false)
                        {
                            Size = new Vector2(100),
                            Position = new Vector2(100),
                        }
                    }
                };
            });

            // Input is positional

            AddStep("move mouse to receptorBelow", () => InputManager.MoveMouseTo(receptorBelow));
            AddStep("press keybind1", () => InputManager.PressKey(Key.Up));
            AddAssert("receptorBelow received press", () => receptorBelow.PressedReceived);

            AddStep("move mouse to receptorAbove", () => InputManager.MoveMouseTo(receptorAbove));
            AddStep("release keybind1", () => InputManager.ReleaseKey(Key.Up));
            AddAssert("receptorBelow received release", () => receptorBelow.ReleasedReceived);
        }

        private class InputReceptor : Box, IKeyBindingHandler<TestKeyBinding>
        {
            public bool PressedReceived { get; private set; }
            public bool ReleasedReceived { get; private set; }

            private readonly bool keybindings;

            public InputReceptor(bool keybindings)
            {
                this.keybindings = keybindings;
            }

            public override bool HandlePositionalInput => true; // IsHovered is used

            protected override bool OnKeyDown(KeyDownEvent e)
            {
                if (keybindings)
                    return false;

                if (!IsHovered)
                    return false;

                return true;
            }

            protected override void OnKeyUp(KeyUpEvent e)
            {
            }

            public bool OnPressed(KeyBindingPressEvent<TestKeyBinding> e)
            {
                if (!keybindings)
                    return false;

                if (!IsHovered)
                    return false;

                PressedReceived = true;
                return true;
            }

            public void OnReleased(KeyBindingReleaseEvent<TestKeyBinding> e)
            {
                if (!keybindings)
                    return;

                ReleasedReceived = true;
            }
        }

        private class TestKeyBindingContainer : KeyBindingContainer<TestKeyBinding>, IHandleGlobalKeyboardInput
        {
            public TestKeyBindingContainer()
                : base(SimultaneousBindingMode.Unique, KeyCombinationMatchingMode.Modifiers)
            {
            }

            public override IEnumerable<IKeyBinding> DefaultKeyBindings => new[]
            {
                new KeyBinding(InputKey.Up, TestKeyBinding.Binding1),
                new KeyBinding(InputKey.Down, TestKeyBinding.Binding2),
            };
        }

        private enum TestKeyBinding
        {
            Binding1,
            Binding2
        }
    }
}
