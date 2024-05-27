// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Input;
using osu.Framework.Input.Bindings;
using osu.Framework.Platform.SDL3;
using osuTK.Input;
using SDL;

namespace osu.Framework.Tests.Platform.SDL3
{
    [TestFixture]
    public class SDLKeyboardTest
    {
        private static SDL_Keysym sym(SDL_Scancode scancode, SDL_Keycode keycode) => new SDL_Keysym
        {
            scancode = scancode,
            sym = keycode,
            mod = SDL_Keymod.SDL_KMOD_NUM
        };

        private static KeyboardKey key(Key key, char? c = null) => new KeyboardKey(key, c);

        private static object[][] testCases =
        {
            // regular left Ctrl
            new object[]
            {
                new[] { sym(SDL_Scancode.SDL_SCANCODE_LCTRL, SDL_Keycode.SDLK_LCTRL) },
                new[] { key(Key.ControlLeft) },
                new[] { InputKey.LControl },
                new[] { InputKey.Control },
            },
            // Ctrl+Z (undo) on QWERTZ keyboard
            new object[]
            {
                new[] { sym(SDL_Scancode.SDL_SCANCODE_LCTRL, SDL_Keycode.SDLK_LCTRL), sym(SDL_Scancode.SDL_SCANCODE_Y, SDL_Keycode.SDLK_z) },
                new[] { key(Key.ControlLeft), key(Key.Y, 'z') },
                new[] { InputKey.LControl, InputKey.Y },
                new[] { InputKey.Control, InputKey.KeycodeZ },
            },
            // regular Backspace
            new object[]
            {
                new[] { sym(SDL_Scancode.SDL_SCANCODE_BACKSPACE, SDL_Keycode.SDLK_BACKSPACE) },
                new[] { key(Key.BackSpace, '\b') },
                new[] { InputKey.BackSpace },
                Array.Empty<InputKey>(),
            },
            // Caps Lock mapped to Backspace on Linux, see https://github.com/ppy/osu-framework/issues/1463#issuecomment-1596885079
            new object[]
            {
                new[] { sym(SDL_Scancode.SDL_SCANCODE_CAPSLOCK, SDL_Keycode.SDLK_BACKSPACE) },
                new[] { key(Key.CapsLock, '\b') }, // should be Backspace
                new[] { InputKey.CapsLock },
                Array.Empty<InputKey>(),
            },

            // swap Ctrl and Caps Lock on linux, see https://github.com/ppy/osu/discussions/23893
            // using: setxkbmap -option "ctrl:swapcaps"
            new object[]
            {
                new[] { sym(SDL_Scancode.SDL_SCANCODE_CAPSLOCK, SDL_Keycode.SDLK_LCTRL) },
                new[] { key(Key.CapsLock) }, // should be Ctrl
                new[] { InputKey.CapsLock },
                Array.Empty<InputKey>(),
            },
            new object[]
            {
                new[] { sym(SDL_Scancode.SDL_SCANCODE_LCTRL, SDL_Keycode.SDLK_CAPSLOCK) },
                new[] { key(Key.ControlLeft) }, // should be Caps Lock
                new[] { InputKey.LControl },
                new[] { InputKey.Control }
            },

            // dvorak keyboard layout, added since it maps some alpha keys to punctuation and vice versa
            new object[]
            {
                new[] { sym(SDL_Scancode.SDL_SCANCODE_W, SDL_Keycode.SDLK_COMMA) },
                new[] { key(Key.W, ',') },
                new[] { InputKey.W },
                Array.Empty<InputKey>(),
            },
            new object[]
            {
                new[] { sym(SDL_Scancode.SDL_SCANCODE_COMMA, SDL_Keycode.SDLK_w) },
                new[] { key(Key.Comma, 'w') },
                new[] { InputKey.Comma },
                new[] { InputKey.KeycodeW },
            },

            // Ctrl swapped with A ...this is just here to show how it behaves, I don't think anyone will actually use it like this
            new object[]
            {
                new[] { sym(SDL_Scancode.SDL_SCANCODE_LCTRL, SDL_Keycode.SDLK_a) },
                new[] { key(Key.ControlLeft, 'a') },
                new[] { InputKey.LControl },
                new[] { InputKey.Control }, // could be KeyCode.A
            },
            new object[]
            {
                new[] { sym(SDL_Scancode.SDL_SCANCODE_A, SDL_Keycode.SDLK_LCTRL) },
                new[] { key(Key.A) },
                new[] { InputKey.A },
                Array.Empty<InputKey>(),
            },
        };

        [TestCaseSource(nameof(testCases))]
        public void TestPressKeys(SDL_Keysym[] sdlKeys, KeyboardKey[] expectedKeys, InputKey[] expectedPhysicalKeys, InputKey[] expectedVirtualKeys)
        {
            List<KeyboardKey> pressedKeys = new List<KeyboardKey>();
            List<InputKey> pressedPhysicalKeys = new List<InputKey>();
            var characters = new Dictionary<Key, char?>();

            foreach (var sdlKey in sdlKeys)
            {
                var pressedKey = sdlKey.ToKeyboardKey();
                Assert.That(pressedKey.Key, Is.Not.EqualTo(Key.Unknown));
                pressedKeys.Add(pressedKey);
                pressedPhysicalKeys.Add(KeyCombination.FromKey(pressedKey.Key));
                characters.Add(pressedKey.Key, pressedKey.Character);
            }

            var pressedVirtualKeys = pressedPhysicalKeys.Select(k => KeyCombination.GetVirtualKey(k, characters)).Where(v => v != null).Cast<InputKey>();

            Assert.That(pressedKeys, Is.EqualTo(expectedKeys));
            Assert.That(pressedPhysicalKeys, Is.EqualTo(expectedPhysicalKeys));
            Assert.That(pressedVirtualKeys, Is.EqualTo(expectedVirtualKeys));
        }
    }
}
