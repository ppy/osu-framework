// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

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
            new object[] { new KeyCombination(InputKey.Shift), new KeyCombination(InputKey.LShift), false, true },
            new object[] { new KeyCombination(InputKey.Shift), new KeyCombination(InputKey.RShift), false, true },
            new object[] { new KeyCombination(InputKey.Shift), new KeyCombination(InputKey.Shift), false, true },
            new object[] { new KeyCombination(InputKey.LShift), new KeyCombination(InputKey.RShift), false, false },
            new object[] { new KeyCombination(InputKey.RShift), new KeyCombination(InputKey.RShift), false, true },

            new object[] { new KeyCombination(InputKey.Shift), new KeyCombination(InputKey.LShift), true, true },
            new object[] { new KeyCombination(InputKey.Shift), new KeyCombination(InputKey.RShift), true, true },
            new object[] { new KeyCombination(InputKey.Shift), new KeyCombination(InputKey.Shift), true, true },
            new object[] { new KeyCombination(InputKey.LShift), new KeyCombination(InputKey.RShift), true, false },
            new object[] { new KeyCombination(InputKey.RShift), new KeyCombination(InputKey.RShift), true, true },

            // test multiple combination matches.
            new object[] { new KeyCombination(InputKey.Shift), new KeyCombination(InputKey.Shift, InputKey.LShift), false, true },
            new object[] { new KeyCombination(InputKey.Shift), new KeyCombination(InputKey.Shift, InputKey.RShift), false, true },
            new object[] { new KeyCombination(InputKey.LShift), new KeyCombination(InputKey.Shift, InputKey.RShift), false, false },
            new object[] { new KeyCombination(InputKey.RShift), new KeyCombination(InputKey.Shift, InputKey.RShift), false, true },

            new object[] { new KeyCombination(InputKey.Shift), new KeyCombination(InputKey.Shift, InputKey.LShift), true, true },
            new object[] { new KeyCombination(InputKey.Shift), new KeyCombination(InputKey.Shift, InputKey.RShift), true, true },
            new object[] { new KeyCombination(InputKey.LShift), new KeyCombination(InputKey.Shift, InputKey.RShift), true, false },
            new object[] { new KeyCombination(InputKey.RShift), new KeyCombination(InputKey.Shift, InputKey.RShift), true, true },
        };

        [TestCaseSource(nameof(key_combination_display_test_cases))]
        public void TestLeftRightModifierHandling(KeyCombination candidate, KeyCombination pressed, bool exactModifiers, bool shouldContain)
            => Assert.AreEqual(shouldContain, KeyCombination.ContainsAll(candidate.Keys, pressed.Keys, exactModifiers));
    }
}
