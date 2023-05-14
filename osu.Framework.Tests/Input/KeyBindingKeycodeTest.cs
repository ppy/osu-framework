// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Input;
using osu.Framework.Input.Bindings;
using osu.Framework.Testing;
using osuTK.Input;

namespace osu.Framework.Tests.Input
{
    public partial class KeyBindingKeycodeTest : ManualInputManagerTestScene
    {
        private TestKeyBindingContainer keyBindingContainer = null!;

        private void create(IEnumerable<IKeyBinding> keyBindings, SimultaneousBindingMode simultaneousMode, KeyCombinationMatchingMode matchingMode)
        {
            AddStep("create hierarchy", () =>
            {
                Child = keyBindingContainer = new TestKeyBindingContainer(keyBindings, simultaneousMode, matchingMode);
            });
        }

        [Test]
        public void TestConflictingKeys()
        {
            create(test_bindings, SimultaneousBindingMode.Unique, KeyCombinationMatchingMode.Modifiers);

            press(new KeyboardKey(Key.A, 'a'));
            check(TestKeyBinding.A, TestKeyBinding.KeycodeA);
            release(new KeyboardKey(Key.A, 'a'));

            press(KeyboardKey.FromKey(Key.LControl));
            press(new KeyboardKey(Key.A, 'a'));
            check(TestKeyBinding.CtrlA, TestKeyBinding.CtrlKeycodeA);
            release(new KeyboardKey(Key.A, 'a'));
            release(KeyboardKey.FromKey(Key.LControl));

            check();
        }

        [TestCase(KeyCombinationMatchingMode.Any)]
        [TestCase(KeyCombinationMatchingMode.Exact)]
        [TestCase(KeyCombinationMatchingMode.Modifiers)]
        public void TestBrokenInExactMode(KeyCombinationMatchingMode mode)
        {
            create(test_bindings, SimultaneousBindingMode.Unique, mode);

            press(new KeyboardKey(Key.U, 'a'));

            if (mode == KeyCombinationMatchingMode.Exact)
                check(); // in an ideal implementation, this would not be empty
            else
                check(TestKeyBinding.KeycodeA);

            release(new KeyboardKey(Key.U, 'a'));

            check();
        }

        [TestCase('d', 'm')]
        [TestCase('m', 'd')] // swap D and M
        public void TestSimpleGame(char keycodeForD, char keycodeForM)
        {
            create(simple_game_bindings, SimultaneousBindingMode.Unique, KeyCombinationMatchingMode.Modifiers);

            var keyD = new KeyboardKey(Key.D, keycodeForD);
            var keyM = new KeyboardKey(Key.M, keycodeForM);

            press(keyD);
            if (keyD.Character == 'm')
                check(TestKeyBinding.Right, TestKeyBinding.Map); // map is also triggered, even if the developer wouldn't expect it
            else
                check(TestKeyBinding.Right);
            release(keyD);

            press(keyM);
            if (keyM.Character == 'm')
                check(TestKeyBinding.Map);
            else
                check(); // empty is expected here
            release(keyM);

            var keyForMenu = keyD.Character == 'm' ? keyD : keyM;

            AddAssert("keycode 'm' will be pressed", () => keyForMenu.Character == 'm');

            press(KeyboardKey.FromKey(Key.ControlLeft));
            press(keyForMenu);
            check(TestKeyBinding.Menu); // since Menu is using modifier keys different from Up/Down/Left/Right, it'll always work as expected
            release(keyForMenu);
            release(KeyboardKey.FromKey(Key.ControlLeft));

            check();
        }

        private void press(KeyboardKey key) => AddStep($"press {key}", () => InputManager.PressKey(key));
        private void release(KeyboardKey key) => AddStep($"release {key}", () => InputManager.ReleaseKey(key));

        private void check(params TestKeyBinding[] bindings) => AddAssert($"check {(bindings.Any() ? string.Join(", ", bindings) : "<empty>")}", () => keyBindingContainer.PressedActions, () => Is.EquivalentTo(bindings));

        private partial class TestKeyBindingContainer : KeyBindingContainer<TestKeyBinding>
        {
            public TestKeyBindingContainer(IEnumerable<IKeyBinding> keyBindings, SimultaneousBindingMode simultaneousMode, KeyCombinationMatchingMode matchingMode)
                : base(simultaneousMode, matchingMode)
            {
                DefaultKeyBindings = keyBindings;
            }

            public override IEnumerable<IKeyBinding> DefaultKeyBindings { get; }
        }

        private static readonly IEnumerable<IKeyBinding> test_bindings = new[]
        {
            new KeyBinding(InputKey.A, TestKeyBinding.A),
            new KeyBinding(InputKey.KeycodeA, TestKeyBinding.KeycodeA),
            new KeyBinding(InputKey.KeycodeB, TestKeyBinding.KeycodeB),
            new KeyBinding(new KeyCombination(InputKey.Control, InputKey.A), TestKeyBinding.CtrlA),
            new KeyBinding(new KeyCombination(InputKey.Control, InputKey.KeycodeA), TestKeyBinding.CtrlKeycodeA),
        };

        /// sample keybinds for a simple game that will not as expected on some layouts
        private static readonly IEnumerable<IKeyBinding> simple_game_bindings = new[]
        {
            new KeyBinding(InputKey.W, TestKeyBinding.Up),
            new KeyBinding(InputKey.A, TestKeyBinding.Left),
            new KeyBinding(InputKey.S, TestKeyBinding.Down),
            new KeyBinding(InputKey.D, TestKeyBinding.Right),
            new KeyBinding(InputKey.KeycodeM, TestKeyBinding.Map),
            new KeyBinding(new KeyCombination(InputKey.Control, InputKey.KeycodeM), TestKeyBinding.Menu),
        };

        private enum TestKeyBinding
        {
            A,
            KeycodeA,
            KeycodeB,
            CtrlA,
            CtrlKeycodeA,

            Up,
            Left,
            Down,
            Right,
            Map,
            Menu,
        }
    }
}
