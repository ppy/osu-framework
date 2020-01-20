// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Framework.Testing;
using osuTK;
using osuTK.Graphics;
using osuTK.Input;

namespace osu.Framework.Tests.Visual
{
    public class TestSceneScratch : ManualInputManagerTestScene
    {
        [Test]
        public void DoTest()
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

            AddStep("move mouse to receptorBelow", () => InputManager.MoveMouseTo(receptorBelow));
            AddStep("press keybind1", () => InputManager.PressKey(Key.Up));

            AddStep("move mouse to reecptorAbove", () => InputManager.MoveMouseTo(receptorAbove));
            AddStep("release keybind1", () => InputManager.ReleaseKey(Key.Up));

            AddStep("move mouse to receptorBelow", () => InputManager.MoveMouseTo(receptorBelow));
            AddStep("press keybind2", () => InputManager.PressKey(Key.Down));
            AddStep("release keybind2", () => InputManager.ReleaseKey(Key.Down));
        }

        private class InputReceptor : Box, IKeyBindingHandler<TestKeyBinding>
        {
            private readonly bool keybindings;

            public InputReceptor(bool keybindings)
            {
                this.keybindings = keybindings;
            }

            public override bool HandlePositionalInput => true;

            protected override bool OnKeyDown(KeyDownEvent e)
            {
                if (keybindings)
                    return false;

                if (!IsHovered)
                    return false;

                this.FlashColour(e.Key == Key.Up ? Color4.Green : Color4.Red, 200);
                return true;
            }

            public bool OnPressed(TestKeyBinding action)
            {
                if (!keybindings)
                    return false;

                if (!IsHovered)
                    return false;

                this.FlashColour(action == TestKeyBinding.Binding1 ? Color4.Green : Color4.Red, 200);
                return true;
            }

            public bool OnReleased(TestKeyBinding action)
            {
                if (!IsHovered)
                    return false;

                return keybindings;
            }
        }

        private class TestKeyBindingContainer : KeyBindingContainer<TestKeyBinding>, IHandleGlobalKeyboardInput
        {
            public TestKeyBindingContainer()
                : base(SimultaneousBindingMode.Unique, KeyCombinationMatchingMode.Modifiers)
            {
            }

            public override IEnumerable<KeyBinding> DefaultKeyBindings => new[]
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
