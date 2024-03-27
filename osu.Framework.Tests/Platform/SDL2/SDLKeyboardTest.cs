// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using NUnit.Framework;
using osu.Framework.Input;
using osu.Framework.Input.Bindings;
using osu.Framework.Platform.SDL2;
using osuTK.Input;
using static SDL2.SDL;

namespace osu.Framework.Tests.Platform.SDL2
{
    [TestFixture]
    public class SDLKeyboardTest
    {
        private static SDL_Keysym sym(SDL_Scancode scancode, SDL_Keycode keycode) => new SDL_Keysym
        {
            scancode = scancode,
            sym = keycode,
            mod = SDL_Keymod.KMOD_NUM
        };

        private static KeyboardKey key(Key key, char? c = null) => new KeyboardKey(key, c);

        private static object[][] testCases =
        {
            // regular left Ctrl
            new object[]
            {
                new[] { sym(SDL_Scancode.SDL_SCANCODE_LCTRL, SDL_Keycode.SDLK_LCTRL) },
                new[] { key(Key.ControlLeft) },
                new[] { InputKey.LControl }
            },
            // Ctrl+Z (undo) on QWERTZ keyboard
            new object[]
            {
                new[] { sym(SDL_Scancode.SDL_SCANCODE_LCTRL, SDL_Keycode.SDLK_LCTRL), sym(SDL_Scancode.SDL_SCANCODE_Y, SDL_Keycode.SDLK_z) },
                new[] { key(Key.ControlLeft), key(Key.Y, 'z') },
                new[] { InputKey.LControl, InputKey.Y, InputKey.KeycodeZ }
            },
            // regular Backspace
            new object[]
            {
                new[] { sym(SDL_Scancode.SDL_SCANCODE_BACKSPACE, SDL_Keycode.SDLK_BACKSPACE) },
                new[] { key(Key.BackSpace, '\b') },
                new[] { InputKey.BackSpace }
            },
            // Caps Lock mapped to Backspace on Linux, see https://github.com/ppy/osu-framework/issues/1463#issuecomment-1596885079
            new object[]
            {
                new[] { sym(SDL_Scancode.SDL_SCANCODE_CAPSLOCK, SDL_Keycode.SDLK_BACKSPACE) },
                new[] { key(Key.CapsLock, '\b') }, // should be Backspace
                new[] { InputKey.CapsLock }
            },

            // swap Ctrl and Caps Lock on linux, see https://github.com/ppy/osu/discussions/23893
            // using: setxkbmap -option "ctrl:swapcaps"
            new object[]
            {
                new[] { sym(SDL_Scancode.SDL_SCANCODE_CAPSLOCK, SDL_Keycode.SDLK_LCTRL) },
                new[] { key(Key.CapsLock) }, // should be Ctrl
                new[] { InputKey.CapsLock }
            },
            new object[]
            {
                new[] { sym(SDL_Scancode.SDL_SCANCODE_LCTRL, SDL_Keycode.SDLK_CAPSLOCK) },
                new[] { key(Key.ControlLeft) }, // should be Caps Lock
                new[] { InputKey.LControl }
            },

            // dvorak keyboard layout, added since it maps some alpha keys to punctuation and vice versa
            new object[]
            {
                new[] { sym(SDL_Scancode.SDL_SCANCODE_W, SDL_Keycode.SDLK_COMMA) },
                new[] { key(Key.W, ',') },
                new[] { InputKey.W }
            },
            new object[]
            {
                new[] { sym(SDL_Scancode.SDL_SCANCODE_COMMA, SDL_Keycode.SDLK_w) },
                new[] { key(Key.Comma, 'w') },
                new[] { InputKey.Comma, InputKey.KeycodeW }
            },

            // Ctrl swapped with A ...this is just here to show how it behaves, I don't think anyone will actually use it like this
            new object[]
            {
                new[] { sym(SDL_Scancode.SDL_SCANCODE_LCTRL, SDL_Keycode.SDLK_a) },
                new[] { key(Key.ControlLeft, 'a') },
                new[] { InputKey.LControl, InputKey.KeycodeA }
            },
            new object[]
            {
                new[] { sym(SDL_Scancode.SDL_SCANCODE_A, SDL_Keycode.SDLK_LCTRL) },
                new[] { key(Key.A) },
                new[] { InputKey.A }
            },
        };

        [TestCaseSource(nameof(testCases))]
        public void TestPressKeys(SDL_Keysym[] sdlKeys, KeyboardKey[] expectedKeys, InputKey[] expectedInputKeys)
        {
            List<KeyboardKey> pressedKeys = new List<KeyboardKey>();
            List<InputKey> pressedInputKeys = new List<InputKey>();

            foreach (var sdlKey in sdlKeys)
            {
                var pressedKey = sdlKey.ToKeyboardKey();
                Assert.That(pressedKey.Key, Is.Not.EqualTo(Key.Unknown));
                pressedKeys.Add(pressedKey);
                pressedInputKeys.AddRange(KeyCombination.FromKey(pressedKey));
            }

            Assert.That(pressedKeys, Is.EqualTo(expectedKeys));
            Assert.That(pressedInputKeys, Is.EqualTo(expectedInputKeys));
        }
    }
}
