// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using NUnit.Framework;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.States;
using osuTK.Input;

namespace osu.Framework.Tests.Input
{
    [TestFixture]
    public class KeyCombinationTest
    {
        private static readonly object[][] key_combination_display_test_cases =
        {
            // test single combination matches.
            new object[] { new KeyCombination(InputKey.Shift), new KeyCombination(InputKey.LShift), KeyCombinationMatchingMode.Any, true },
            new object[] { new KeyCombination(InputKey.Shift), new KeyCombination(InputKey.RShift), KeyCombinationMatchingMode.Any, true },
            new object[] { new KeyCombination(InputKey.LShift), new KeyCombination(InputKey.RShift), KeyCombinationMatchingMode.Any, false },
            new object[] { new KeyCombination(InputKey.RShift), new KeyCombination(InputKey.RShift), KeyCombinationMatchingMode.Any, true },

            new object[] { new KeyCombination(InputKey.Shift), new KeyCombination(InputKey.LShift), KeyCombinationMatchingMode.Exact, true },
            new object[] { new KeyCombination(InputKey.Shift), new KeyCombination(InputKey.RShift), KeyCombinationMatchingMode.Exact, true },
            new object[] { new KeyCombination(InputKey.LShift), new KeyCombination(InputKey.RShift), KeyCombinationMatchingMode.Exact, false },
            new object[] { new KeyCombination(InputKey.RShift), new KeyCombination(InputKey.RShift), KeyCombinationMatchingMode.Exact, true },

            new object[] { new KeyCombination(InputKey.Shift), new KeyCombination(InputKey.LShift), KeyCombinationMatchingMode.Modifiers, true },
            new object[] { new KeyCombination(InputKey.Shift), new KeyCombination(InputKey.RShift), KeyCombinationMatchingMode.Modifiers, true },
            new object[] { new KeyCombination(InputKey.LShift), new KeyCombination(InputKey.RShift), KeyCombinationMatchingMode.Modifiers, false },
            new object[] { new KeyCombination(InputKey.RShift), new KeyCombination(InputKey.RShift), KeyCombinationMatchingMode.Modifiers, true },

            // test multiple combination matches.
            new object[] { new KeyCombination(InputKey.Shift), new KeyCombination(InputKey.LShift, InputKey.RShift), KeyCombinationMatchingMode.Any, true },
            new object[] { new KeyCombination(InputKey.LShift), new KeyCombination(InputKey.LShift, InputKey.RShift), KeyCombinationMatchingMode.Any, true },
            new object[] { new KeyCombination(InputKey.RShift), new KeyCombination(InputKey.LShift, InputKey.RShift), KeyCombinationMatchingMode.Any, true },
            new object[] { new KeyCombination(InputKey.RShift), new KeyCombination(InputKey.RShift, InputKey.A), KeyCombinationMatchingMode.Any, true },

            new object[] { new KeyCombination(InputKey.Shift), new KeyCombination(InputKey.LShift, InputKey.RShift), KeyCombinationMatchingMode.Exact, true },
            new object[] { new KeyCombination(InputKey.LShift), new KeyCombination(InputKey.LShift, InputKey.RShift), KeyCombinationMatchingMode.Exact, false },
            new object[] { new KeyCombination(InputKey.RShift), new KeyCombination(InputKey.LShift, InputKey.RShift), KeyCombinationMatchingMode.Exact, false },
            new object[] { new KeyCombination(InputKey.RShift), new KeyCombination(InputKey.RShift, InputKey.A), KeyCombinationMatchingMode.Exact, false },

            new object[] { new KeyCombination(InputKey.Shift), new KeyCombination(InputKey.LShift, InputKey.RShift), KeyCombinationMatchingMode.Modifiers, true },
            new object[] { new KeyCombination(InputKey.LShift), new KeyCombination(InputKey.LShift, InputKey.RShift), KeyCombinationMatchingMode.Modifiers, false },
            new object[] { new KeyCombination(InputKey.RShift), new KeyCombination(InputKey.LShift, InputKey.RShift), KeyCombinationMatchingMode.Modifiers, false },
            new object[] { new KeyCombination(InputKey.RShift), new KeyCombination(InputKey.RShift, InputKey.A), KeyCombinationMatchingMode.Modifiers, true },
        };

        [TestCaseSource(nameof(key_combination_display_test_cases))]
        public void TestLeftRightModifierHandling(KeyCombination candidate, KeyCombination pressed, KeyCombinationMatchingMode matchingMode, bool shouldContain)
            => Assert.AreEqual(shouldContain, KeyCombination.ContainsAll(candidate.Keys, pressed.Keys, matchingMode));

        [Test]
        public void TestCreationNoDuplicates()
        {
            var keyCombination = new KeyCombination(InputKey.A, InputKey.Control);

            Assert.That(keyCombination.Keys[0], Is.EqualTo(InputKey.Control));
            Assert.That(keyCombination.Keys[1], Is.EqualTo(InputKey.A));
        }

        [Test]
        public void TestCreationWithDuplicates()
        {
            var keyCombination = new KeyCombination(InputKey.A, InputKey.Control, InputKey.A);

            Assert.That(keyCombination.Keys[0], Is.EqualTo(InputKey.Control));
            Assert.That(keyCombination.Keys[1], Is.EqualTo(InputKey.A));
        }

        [Test]
        public void TestEmptyCombinationIsNeverPressed()
        {
            var keyCombination = new KeyCombination(Array.Empty<InputKey>());

            var state = new InputState();
            state.Keyboard.Keys.SetPressed(Key.X, true);

            Assert.That(keyCombination.IsPressed(new KeyCombination(InputKey.X), state, KeyCombinationMatchingMode.Any), Is.False);
        }
    }
}
