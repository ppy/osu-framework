// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.States;
using osuTK.Input;
using KeyboardState = osu.Framework.Input.States.KeyboardState;

namespace osu.Framework.Tests.Input
{
    [TestFixture]
    public class KeyCombinationTest
    {
        [Test]
        public void TestKeyCombinationDisplayTrueOrder()
        {
            var keyCombination1 = new KeyCombination(InputKey.Control, InputKey.Shift, InputKey.R);
            var keyCombination2 = new KeyCombination(InputKey.R, InputKey.Shift, InputKey.Control);

            Assert.AreEqual(keyCombination1.ReadableString(), keyCombination2.ReadableString());
        }

        [Test]
        public void TestKeyCombinationFromKeyboardStateDisplayTrueOrder()
        {
            var keyboardState = new KeyboardState();

            keyboardState.Keys.Add(Key.R);
            keyboardState.Keys.Add(Key.LShift);
            keyboardState.Keys.Add(Key.LControl);

            var keyCombination1 = KeyCombination.FromInputState(new InputState(keyboard: keyboardState));
            var keyCombination2 = new KeyCombination(InputKey.Control, InputKey.Shift, InputKey.R);

            Assert.AreEqual(keyCombination1.ReadableString(), keyCombination2.ReadableString());
        }
    }
}
