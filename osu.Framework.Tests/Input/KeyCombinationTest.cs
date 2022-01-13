// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Input.Bindings;

namespace osu.Framework.Tests.Input
{
    [TestFixture]
    public class KeyCombinationTest
    {
        private static readonly object[][] key_combination_display_test_cases =
        {
            new object[] { new KeyCombination(InputKey.Alt, InputKey.F4), "Alt-F4" },
            new object[] { new KeyCombination(InputKey.D, InputKey.Control), "Ctrl-D" },
            new object[] { new KeyCombination(InputKey.Shift, InputKey.F, InputKey.Control), "Ctrl-Shift-F" },
            new object[] { new KeyCombination(InputKey.Alt, InputKey.Control, InputKey.Super, InputKey.Shift), "Ctrl-Alt-Shift-Win" },
            new object[] { new KeyCombination(InputKey.LAlt, InputKey.F4), "LAlt-F4" },
            new object[] { new KeyCombination(InputKey.D, InputKey.LControl), "LCtrl-D" },
            new object[] { new KeyCombination(InputKey.LShift, InputKey.F, InputKey.LControl), "LCtrl-LShift-F" },
            new object[] { new KeyCombination(InputKey.LAlt, InputKey.LControl, InputKey.LSuper, InputKey.LShift), "LCtrl-LAlt-LShift-LWin" },
            new object[] { new KeyCombination(InputKey.Alt, InputKey.LAlt, InputKey.RControl, InputKey.A), "RCtrl-LAlt-A" },
            new object[] { new KeyCombination(InputKey.Shift, InputKey.LControl, InputKey.X), "LCtrl-Shift-X" },
            new object[] { new KeyCombination(InputKey.Control, InputKey.Shift, InputKey.Alt, InputKey.Super, InputKey.LAlt, InputKey.RShift, InputKey.LSuper), "Ctrl-LAlt-RShift-LWin" },
        };

        [TestCaseSource(nameof(key_combination_display_test_cases))]
        public void TestKeyCombinationDisplayOrder(KeyCombination keyCombination, string expectedRepresentation)
            => Assert.That(keyCombination.ReadableString(), Is.EqualTo(expectedRepresentation));
    }
}
