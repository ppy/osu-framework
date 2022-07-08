// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using NUnit.Framework;
using osu.Framework.Input.Bindings;

namespace osu.Framework.Tests.Input
{
    [TestFixture]
    public class KeyCombinationModifierTest
    {
        private static readonly object[][] key_combination_display_test_cases =
        {
            // test single combination matches.
            new object[] { new KeyCombination(InputKey.Shift), new KeyCombination(InputKey.LShift), KeyCombinationMatchingMode.Any, true },
            new object[] { new KeyCombination(InputKey.Shift), new KeyCombination(InputKey.RShift), KeyCombinationMatchingMode.Any, true },
            new object[] { new KeyCombination(InputKey.Shift), new KeyCombination(InputKey.Shift), KeyCombinationMatchingMode.Any, true },
            new object[] { new KeyCombination(InputKey.LShift), new KeyCombination(InputKey.RShift), KeyCombinationMatchingMode.Any, false },
            new object[] { new KeyCombination(InputKey.RShift), new KeyCombination(InputKey.RShift), KeyCombinationMatchingMode.Any, true },

            new object[] { new KeyCombination(InputKey.Shift), new KeyCombination(InputKey.LShift), KeyCombinationMatchingMode.Exact, true },
            new object[] { new KeyCombination(InputKey.Shift), new KeyCombination(InputKey.RShift), KeyCombinationMatchingMode.Exact, true },
            new object[] { new KeyCombination(InputKey.Shift), new KeyCombination(InputKey.Shift), KeyCombinationMatchingMode.Exact, true },
            new object[] { new KeyCombination(InputKey.LShift), new KeyCombination(InputKey.RShift), KeyCombinationMatchingMode.Exact, false },
            new object[] { new KeyCombination(InputKey.RShift), new KeyCombination(InputKey.RShift), KeyCombinationMatchingMode.Exact, true },

            new object[] { new KeyCombination(InputKey.Shift), new KeyCombination(InputKey.LShift), KeyCombinationMatchingMode.Modifiers, true },
            new object[] { new KeyCombination(InputKey.Shift), new KeyCombination(InputKey.RShift), KeyCombinationMatchingMode.Modifiers, true },
            new object[] { new KeyCombination(InputKey.Shift), new KeyCombination(InputKey.Shift), KeyCombinationMatchingMode.Modifiers, true },
            new object[] { new KeyCombination(InputKey.LShift), new KeyCombination(InputKey.RShift), KeyCombinationMatchingMode.Modifiers, false },
            new object[] { new KeyCombination(InputKey.RShift), new KeyCombination(InputKey.RShift), KeyCombinationMatchingMode.Modifiers, true },

            // test multiple combination matches.
            new object[] { new KeyCombination(InputKey.Shift), new KeyCombination(InputKey.Shift, InputKey.LShift), KeyCombinationMatchingMode.Any, true },
            new object[] { new KeyCombination(InputKey.Shift), new KeyCombination(InputKey.Shift, InputKey.RShift), KeyCombinationMatchingMode.Any, true },
            new object[] { new KeyCombination(InputKey.LShift), new KeyCombination(InputKey.Shift, InputKey.RShift), KeyCombinationMatchingMode.Any, false },
            new object[] { new KeyCombination(InputKey.RShift), new KeyCombination(InputKey.Shift, InputKey.RShift), KeyCombinationMatchingMode.Any, true },

            new object[] { new KeyCombination(InputKey.Shift), new KeyCombination(InputKey.Shift, InputKey.LShift), KeyCombinationMatchingMode.Exact, true },
            new object[] { new KeyCombination(InputKey.Shift), new KeyCombination(InputKey.Shift, InputKey.RShift), KeyCombinationMatchingMode.Exact, true },
            new object[] { new KeyCombination(InputKey.LShift), new KeyCombination(InputKey.Shift, InputKey.RShift), KeyCombinationMatchingMode.Exact, false },
            new object[] { new KeyCombination(InputKey.RShift), new KeyCombination(InputKey.Shift, InputKey.RShift), KeyCombinationMatchingMode.Exact, true },

            new object[] { new KeyCombination(InputKey.Shift), new KeyCombination(InputKey.Shift, InputKey.LShift), KeyCombinationMatchingMode.Modifiers, true },
            new object[] { new KeyCombination(InputKey.Shift), new KeyCombination(InputKey.Shift, InputKey.RShift), KeyCombinationMatchingMode.Modifiers, true },
            new object[] { new KeyCombination(InputKey.LShift), new KeyCombination(InputKey.Shift, InputKey.RShift), KeyCombinationMatchingMode.Modifiers, false },
            new object[] { new KeyCombination(InputKey.RShift), new KeyCombination(InputKey.Shift, InputKey.RShift), KeyCombinationMatchingMode.Modifiers, true },
        };

        [TestCaseSource(nameof(key_combination_display_test_cases))]
        public void TestLeftRightModifierHandling(KeyCombination candidate, KeyCombination pressed, KeyCombinationMatchingMode matchingMode, bool shouldContain)
            => Assert.AreEqual(shouldContain, KeyCombination.ContainsAll(candidate.Keys, pressed.Keys, matchingMode));
    }
}
