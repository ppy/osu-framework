// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Linq;
using osu.Framework.Development;
using osu.Framework.Extensions.EnumExtensions;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Input;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.StateChanges;
using osu.Framework.Logging;
using osuTK.Input;
using SDL;
using static SDL.SDL3;

namespace osu.Framework.Platform.SDL3
{
    public static class SDL3Extensions
    {
        private static readonly ImmutableHashSet<(InputKey, Key, SDL_Keycode, SDL_Scancode)> key_mapping = ImmutableHashSet.Create(
            (InputKey.Enter, Key.Enter, SDL_Keycode.SDLK_RETURN, SDL_Scancode.SDL_SCANCODE_RETURN),
            (InputKey.Escape, Key.Escape, SDL_Keycode.SDLK_ESCAPE, SDL_Scancode.SDL_SCANCODE_ESCAPE),
            (InputKey.BackSpace, Key.BackSpace, SDL_Keycode.SDLK_BACKSPACE, SDL_Scancode.SDL_SCANCODE_BACKSPACE),
            (InputKey.Tab, Key.Tab, SDL_Keycode.SDLK_TAB, SDL_Scancode.SDL_SCANCODE_TAB),
            (InputKey.Space, Key.Space, SDL_Keycode.SDLK_SPACE, SDL_Scancode.SDL_SCANCODE_SPACE),
            (InputKey.Quote, Key.Quote, SDL_Keycode.SDLK_APOSTROPHE, SDL_Scancode.SDL_SCANCODE_APOSTROPHE),
            (InputKey.Comma, Key.Comma, SDL_Keycode.SDLK_COMMA, SDL_Scancode.SDL_SCANCODE_COMMA),
            (InputKey.Minus, Key.Minus, SDL_Keycode.SDLK_MINUS, SDL_Scancode.SDL_SCANCODE_MINUS),
            (InputKey.Period, Key.Period, SDL_Keycode.SDLK_PERIOD, SDL_Scancode.SDL_SCANCODE_PERIOD),
            (InputKey.Slash, Key.Slash, SDL_Keycode.SDLK_SLASH, SDL_Scancode.SDL_SCANCODE_SLASH),
            (InputKey.Number0, Key.Number0, SDL_Keycode.SDLK_0, SDL_Scancode.SDL_SCANCODE_0),
            (InputKey.Number1, Key.Number1, SDL_Keycode.SDLK_1, SDL_Scancode.SDL_SCANCODE_1),
            (InputKey.Number2, Key.Number2, SDL_Keycode.SDLK_2, SDL_Scancode.SDL_SCANCODE_2),
            (InputKey.Number3, Key.Number3, SDL_Keycode.SDLK_3, SDL_Scancode.SDL_SCANCODE_3),
            (InputKey.Number4, Key.Number4, SDL_Keycode.SDLK_4, SDL_Scancode.SDL_SCANCODE_4),
            (InputKey.Number5, Key.Number5, SDL_Keycode.SDLK_5, SDL_Scancode.SDL_SCANCODE_5),
            (InputKey.Number6, Key.Number6, SDL_Keycode.SDLK_6, SDL_Scancode.SDL_SCANCODE_6),
            (InputKey.Number7, Key.Number7, SDL_Keycode.SDLK_7, SDL_Scancode.SDL_SCANCODE_7),
            (InputKey.Number8, Key.Number8, SDL_Keycode.SDLK_8, SDL_Scancode.SDL_SCANCODE_8),
            (InputKey.Number9, Key.Number9, SDL_Keycode.SDLK_9, SDL_Scancode.SDL_SCANCODE_9),
            (InputKey.Semicolon, Key.Semicolon, SDL_Keycode.SDLK_SEMICOLON, SDL_Scancode.SDL_SCANCODE_SEMICOLON),
            (InputKey.Plus, Key.Plus, SDL_Keycode.SDLK_EQUALS, SDL_Scancode.SDL_SCANCODE_EQUALS),
            (InputKey.BracketLeft, Key.BracketLeft, SDL_Keycode.SDLK_LEFTBRACKET, SDL_Scancode.SDL_SCANCODE_LEFTBRACKET),
            (InputKey.BackSlash, Key.BackSlash, SDL_Keycode.SDLK_BACKSLASH, SDL_Scancode.SDL_SCANCODE_BACKSLASH),
            (InputKey.BracketRight, Key.BracketRight, SDL_Keycode.SDLK_RIGHTBRACKET, SDL_Scancode.SDL_SCANCODE_RIGHTBRACKET),
            (InputKey.Tilde, Key.Tilde, SDL_Keycode.SDLK_GRAVE, SDL_Scancode.SDL_SCANCODE_GRAVE),
            (InputKey.A, Key.A, SDL_Keycode.SDLK_A, SDL_Scancode.SDL_SCANCODE_A),
            (InputKey.B, Key.B, SDL_Keycode.SDLK_B, SDL_Scancode.SDL_SCANCODE_B),
            (InputKey.C, Key.C, SDL_Keycode.SDLK_C, SDL_Scancode.SDL_SCANCODE_C),
            (InputKey.D, Key.D, SDL_Keycode.SDLK_D, SDL_Scancode.SDL_SCANCODE_D),
            (InputKey.E, Key.E, SDL_Keycode.SDLK_E, SDL_Scancode.SDL_SCANCODE_E),
            (InputKey.F, Key.F, SDL_Keycode.SDLK_F, SDL_Scancode.SDL_SCANCODE_F),
            (InputKey.G, Key.G, SDL_Keycode.SDLK_G, SDL_Scancode.SDL_SCANCODE_G),
            (InputKey.H, Key.H, SDL_Keycode.SDLK_H, SDL_Scancode.SDL_SCANCODE_H),
            (InputKey.I, Key.I, SDL_Keycode.SDLK_I, SDL_Scancode.SDL_SCANCODE_I),
            (InputKey.J, Key.J, SDL_Keycode.SDLK_J, SDL_Scancode.SDL_SCANCODE_J),
            (InputKey.K, Key.K, SDL_Keycode.SDLK_K, SDL_Scancode.SDL_SCANCODE_K),
            (InputKey.L, Key.L, SDL_Keycode.SDLK_L, SDL_Scancode.SDL_SCANCODE_L),
            (InputKey.M, Key.M, SDL_Keycode.SDLK_M, SDL_Scancode.SDL_SCANCODE_M),
            (InputKey.N, Key.N, SDL_Keycode.SDLK_N, SDL_Scancode.SDL_SCANCODE_N),
            (InputKey.O, Key.O, SDL_Keycode.SDLK_O, SDL_Scancode.SDL_SCANCODE_O),
            (InputKey.P, Key.P, SDL_Keycode.SDLK_P, SDL_Scancode.SDL_SCANCODE_P),
            (InputKey.Q, Key.Q, SDL_Keycode.SDLK_Q, SDL_Scancode.SDL_SCANCODE_Q),
            (InputKey.R, Key.R, SDL_Keycode.SDLK_R, SDL_Scancode.SDL_SCANCODE_R),
            (InputKey.S, Key.S, SDL_Keycode.SDLK_S, SDL_Scancode.SDL_SCANCODE_S),
            (InputKey.T, Key.T, SDL_Keycode.SDLK_T, SDL_Scancode.SDL_SCANCODE_T),
            (InputKey.U, Key.U, SDL_Keycode.SDLK_U, SDL_Scancode.SDL_SCANCODE_U),
            (InputKey.V, Key.V, SDL_Keycode.SDLK_V, SDL_Scancode.SDL_SCANCODE_V),
            (InputKey.W, Key.W, SDL_Keycode.SDLK_W, SDL_Scancode.SDL_SCANCODE_W),
            (InputKey.X, Key.X, SDL_Keycode.SDLK_X, SDL_Scancode.SDL_SCANCODE_X),
            (InputKey.Y, Key.Y, SDL_Keycode.SDLK_Y, SDL_Scancode.SDL_SCANCODE_Y),
            (InputKey.Z, Key.Z, SDL_Keycode.SDLK_Z, SDL_Scancode.SDL_SCANCODE_Z),
            (InputKey.CapsLock, Key.CapsLock, SDL_Keycode.SDLK_CAPSLOCK, SDL_Scancode.SDL_SCANCODE_CAPSLOCK),
            (InputKey.F1, Key.F1, SDL_Keycode.SDLK_F1, SDL_Scancode.SDL_SCANCODE_F1),
            (InputKey.F2, Key.F2, SDL_Keycode.SDLK_F2, SDL_Scancode.SDL_SCANCODE_F2),
            (InputKey.F3, Key.F3, SDL_Keycode.SDLK_F3, SDL_Scancode.SDL_SCANCODE_F3),
            (InputKey.F4, Key.F4, SDL_Keycode.SDLK_F4, SDL_Scancode.SDL_SCANCODE_F4),
            (InputKey.F5, Key.F5, SDL_Keycode.SDLK_F5, SDL_Scancode.SDL_SCANCODE_F5),
            (InputKey.F6, Key.F6, SDL_Keycode.SDLK_F6, SDL_Scancode.SDL_SCANCODE_F6),
            (InputKey.F7, Key.F7, SDL_Keycode.SDLK_F7, SDL_Scancode.SDL_SCANCODE_F7),
            (InputKey.F8, Key.F8, SDL_Keycode.SDLK_F8, SDL_Scancode.SDL_SCANCODE_F8),
            (InputKey.F9, Key.F9, SDL_Keycode.SDLK_F9, SDL_Scancode.SDL_SCANCODE_F9),
            (InputKey.F10, Key.F10, SDL_Keycode.SDLK_F10, SDL_Scancode.SDL_SCANCODE_F10),
            (InputKey.F11, Key.F11, SDL_Keycode.SDLK_F11, SDL_Scancode.SDL_SCANCODE_F11),
            (InputKey.F12, Key.F12, SDL_Keycode.SDLK_F12, SDL_Scancode.SDL_SCANCODE_F12),
            (InputKey.PrintScreen, Key.PrintScreen, SDL_Keycode.SDLK_PRINTSCREEN, SDL_Scancode.SDL_SCANCODE_PRINTSCREEN),
            (InputKey.ScrollLock, Key.ScrollLock, SDL_Keycode.SDLK_SCROLLLOCK, SDL_Scancode.SDL_SCANCODE_SCROLLLOCK),
            (InputKey.Pause, Key.Pause, SDL_Keycode.SDLK_PAUSE, SDL_Scancode.SDL_SCANCODE_PAUSE),
            (InputKey.Insert, Key.Insert, SDL_Keycode.SDLK_INSERT, SDL_Scancode.SDL_SCANCODE_INSERT),
            (InputKey.Home, Key.Home, SDL_Keycode.SDLK_HOME, SDL_Scancode.SDL_SCANCODE_HOME),
            (InputKey.PageUp, Key.PageUp, SDL_Keycode.SDLK_PAGEUP, SDL_Scancode.SDL_SCANCODE_PAGEUP),
            (InputKey.Delete, Key.Delete, SDL_Keycode.SDLK_DELETE, SDL_Scancode.SDL_SCANCODE_DELETE),
            (InputKey.End, Key.End, SDL_Keycode.SDLK_END, SDL_Scancode.SDL_SCANCODE_END),
            (InputKey.PageDown, Key.PageDown, SDL_Keycode.SDLK_PAGEDOWN, SDL_Scancode.SDL_SCANCODE_PAGEDOWN),
            (InputKey.Right, Key.Right, SDL_Keycode.SDLK_RIGHT, SDL_Scancode.SDL_SCANCODE_RIGHT),
            (InputKey.Left, Key.Left, SDL_Keycode.SDLK_LEFT, SDL_Scancode.SDL_SCANCODE_LEFT),
            (InputKey.Down, Key.Down, SDL_Keycode.SDLK_DOWN, SDL_Scancode.SDL_SCANCODE_DOWN),
            (InputKey.Up, Key.Up, SDL_Keycode.SDLK_UP, SDL_Scancode.SDL_SCANCODE_UP),
            (InputKey.NumLock, Key.NumLock, SDL_Keycode.SDLK_NUMLOCKCLEAR, SDL_Scancode.SDL_SCANCODE_NUMLOCKCLEAR),
            (InputKey.KeypadDivide, Key.KeypadDivide, SDL_Keycode.SDLK_KP_DIVIDE, SDL_Scancode.SDL_SCANCODE_KP_DIVIDE),
            (InputKey.KeypadMultiply, Key.KeypadMultiply, SDL_Keycode.SDLK_KP_MULTIPLY, SDL_Scancode.SDL_SCANCODE_KP_MULTIPLY),
            (InputKey.KeypadMinus, Key.KeypadMinus, SDL_Keycode.SDLK_KP_MINUS, SDL_Scancode.SDL_SCANCODE_KP_MINUS),
            (InputKey.KeypadPlus, Key.KeypadPlus, SDL_Keycode.SDLK_KP_PLUS, SDL_Scancode.SDL_SCANCODE_KP_PLUS),
            (InputKey.KeypadEnter, Key.KeypadEnter, SDL_Keycode.SDLK_KP_ENTER, SDL_Scancode.SDL_SCANCODE_KP_ENTER),
            (InputKey.Keypad1, Key.Keypad1, SDL_Keycode.SDLK_KP_1, SDL_Scancode.SDL_SCANCODE_KP_1),
            (InputKey.Keypad2, Key.Keypad2, SDL_Keycode.SDLK_KP_2, SDL_Scancode.SDL_SCANCODE_KP_2),
            (InputKey.Keypad3, Key.Keypad3, SDL_Keycode.SDLK_KP_3, SDL_Scancode.SDL_SCANCODE_KP_3),
            (InputKey.Keypad4, Key.Keypad4, SDL_Keycode.SDLK_KP_4, SDL_Scancode.SDL_SCANCODE_KP_4),
            (InputKey.Keypad5, Key.Keypad5, SDL_Keycode.SDLK_KP_5, SDL_Scancode.SDL_SCANCODE_KP_5),
            (InputKey.Keypad6, Key.Keypad6, SDL_Keycode.SDLK_KP_6, SDL_Scancode.SDL_SCANCODE_KP_6),
            (InputKey.Keypad7, Key.Keypad7, SDL_Keycode.SDLK_KP_7, SDL_Scancode.SDL_SCANCODE_KP_7),
            (InputKey.Keypad8, Key.Keypad8, SDL_Keycode.SDLK_KP_8, SDL_Scancode.SDL_SCANCODE_KP_8),
            (InputKey.Keypad9, Key.Keypad9, SDL_Keycode.SDLK_KP_9, SDL_Scancode.SDL_SCANCODE_KP_9),
            (InputKey.Keypad0, Key.Keypad0, SDL_Keycode.SDLK_KP_0, SDL_Scancode.SDL_SCANCODE_KP_0),
            (InputKey.KeypadPeriod, Key.KeypadPeriod, SDL_Keycode.SDLK_KP_PERIOD, SDL_Scancode.SDL_SCANCODE_KP_PERIOD),
            (InputKey.NonUSBackSlash, Key.NonUSBackSlash, SDL_Keycode.SDLK_UNKNOWN, SDL_Scancode.SDL_SCANCODE_NONUSBACKSLASH),
            (InputKey.Menu, Key.Menu, SDL_Keycode.SDLK_MENU, SDL_Scancode.SDL_SCANCODE_MENU),
            (InputKey.None, Key.Menu, SDL_Keycode.SDLK_APPLICATION, SDL_Scancode.SDL_SCANCODE_APPLICATION),
            (InputKey.None, Key.Menu, SDL_Keycode.SDLK_MODE, SDL_Scancode.SDL_SCANCODE_MODE),
            (InputKey.Mute, Key.Mute, SDL_Keycode.SDLK_MUTE, SDL_Scancode.SDL_SCANCODE_MUTE),
            (InputKey.VolumeUp, Key.VolumeUp, SDL_Keycode.SDLK_VOLUMEUP, SDL_Scancode.SDL_SCANCODE_VOLUMEUP),
            (InputKey.VolumeDown, Key.VolumeDown, SDL_Keycode.SDLK_VOLUMEDOWN, SDL_Scancode.SDL_SCANCODE_VOLUMEDOWN),
            (InputKey.Clear, Key.Clear, SDL_Keycode.SDLK_CLEAR, SDL_Scancode.SDL_SCANCODE_CLEAR),
            (InputKey.LControl, Key.ControlLeft, SDL_Keycode.SDLK_LCTRL, SDL_Scancode.SDL_SCANCODE_LCTRL),
            (InputKey.LShift, Key.ShiftLeft, SDL_Keycode.SDLK_LSHIFT, SDL_Scancode.SDL_SCANCODE_LSHIFT),
            (InputKey.LAlt, Key.AltLeft, SDL_Keycode.SDLK_LALT, SDL_Scancode.SDL_SCANCODE_LALT),
            (InputKey.LSuper, Key.WinLeft, SDL_Keycode.SDLK_LGUI, SDL_Scancode.SDL_SCANCODE_LGUI),
            (InputKey.RControl, Key.ControlRight, SDL_Keycode.SDLK_RCTRL, SDL_Scancode.SDL_SCANCODE_RCTRL),
            (InputKey.RShift, Key.ShiftRight, SDL_Keycode.SDLK_RSHIFT, SDL_Scancode.SDL_SCANCODE_RSHIFT),
            (InputKey.RAlt, Key.AltRight, SDL_Keycode.SDLK_RALT, SDL_Scancode.SDL_SCANCODE_RALT),
            (InputKey.RSuper, Key.WinRight, SDL_Keycode.SDLK_RGUI, SDL_Scancode.SDL_SCANCODE_RGUI),
            (InputKey.TrackNext, Key.TrackNext, SDL_Keycode.SDLK_MEDIA_NEXT_TRACK, SDL_Scancode.SDL_SCANCODE_MEDIA_NEXT_TRACK),
            (InputKey.TrackPrevious, Key.TrackPrevious, SDL_Keycode.SDLK_MEDIA_PREVIOUS_TRACK, SDL_Scancode.SDL_SCANCODE_MEDIA_PREVIOUS_TRACK),
            (InputKey.Stop, Key.Stop, SDL_Keycode.SDLK_MEDIA_STOP, SDL_Scancode.SDL_SCANCODE_MEDIA_STOP),
            (InputKey.None, Key.Stop, SDL_Keycode.SDLK_STOP, SDL_Scancode.SDL_SCANCODE_STOP),
            (InputKey.PlayPause, Key.PlayPause, SDL_Keycode.SDLK_MEDIA_PLAY_PAUSE, SDL_Scancode.SDL_SCANCODE_MEDIA_PLAY_PAUSE),
            (InputKey.Sleep, Key.Sleep, SDL_Keycode.SDLK_SLEEP, SDL_Scancode.SDL_SCANCODE_SLEEP),
            (InputKey.None, Key.Escape, SDL_Keycode.SDLK_AC_BACK, SDL_Scancode.SDL_SCANCODE_AC_BACK),
            (InputKey.None, Key.Comma, SDL_Keycode.SDLK_KP_COMMA, SDL_Scancode.SDL_SCANCODE_KP_COMMA),
            (InputKey.None, Key.Tab, SDL_Keycode.SDLK_KP_TAB, SDL_Scancode.SDL_SCANCODE_KP_TAB),
            (InputKey.None, Key.BackSpace, SDL_Keycode.SDLK_KP_BACKSPACE, SDL_Scancode.SDL_SCANCODE_KP_BACKSPACE),
            (InputKey.None, Key.A, SDL_Keycode.SDLK_KP_A, SDL_Scancode.SDL_SCANCODE_KP_A),
            (InputKey.None, Key.B, SDL_Keycode.SDLK_KP_B, SDL_Scancode.SDL_SCANCODE_KP_B),
            (InputKey.None, Key.C, SDL_Keycode.SDLK_KP_C, SDL_Scancode.SDL_SCANCODE_KP_C),
            (InputKey.None, Key.D, SDL_Keycode.SDLK_KP_D, SDL_Scancode.SDL_SCANCODE_KP_D),
            (InputKey.None, Key.E, SDL_Keycode.SDLK_KP_E, SDL_Scancode.SDL_SCANCODE_KP_E),
            (InputKey.None, Key.F, SDL_Keycode.SDLK_KP_F, SDL_Scancode.SDL_SCANCODE_KP_F),
            (InputKey.None, Key.Space, SDL_Keycode.SDLK_KP_SPACE, SDL_Scancode.SDL_SCANCODE_KP_SPACE),
            (InputKey.None, Key.Clear, SDL_Keycode.SDLK_KP_CLEAR, SDL_Scancode.SDL_SCANCODE_KP_CLEAR),
            (InputKey.None, Key.KeypadDecimal, SDL_Keycode.SDLK_DECIMALSEPARATOR, SDL_Scancode.SDL_SCANCODE_DECIMALSEPARATOR),
            (InputKey.F13, Key.F13, SDL_Keycode.SDLK_F13, SDL_Scancode.SDL_SCANCODE_F13),
            (InputKey.F14, Key.F14, SDL_Keycode.SDLK_F14, SDL_Scancode.SDL_SCANCODE_F14),
            (InputKey.F15, Key.F15, SDL_Keycode.SDLK_F15, SDL_Scancode.SDL_SCANCODE_F15),
            (InputKey.F16, Key.F16, SDL_Keycode.SDLK_F16, SDL_Scancode.SDL_SCANCODE_F16),
            (InputKey.F17, Key.F17, SDL_Keycode.SDLK_F17, SDL_Scancode.SDL_SCANCODE_F17),
            (InputKey.F18, Key.F18, SDL_Keycode.SDLK_F18, SDL_Scancode.SDL_SCANCODE_F18),
            (InputKey.F19, Key.F19, SDL_Keycode.SDLK_F19, SDL_Scancode.SDL_SCANCODE_F19),
            (InputKey.F20, Key.F20, SDL_Keycode.SDLK_F20, SDL_Scancode.SDL_SCANCODE_F20),
            (InputKey.F21, Key.F21, SDL_Keycode.SDLK_F21, SDL_Scancode.SDL_SCANCODE_F21),
            (InputKey.F22, Key.F22, SDL_Keycode.SDLK_F22, SDL_Scancode.SDL_SCANCODE_F22),
            (InputKey.F23, Key.F23, SDL_Keycode.SDLK_F23, SDL_Scancode.SDL_SCANCODE_F23),
            (InputKey.F24, Key.F24, SDL_Keycode.SDLK_F24, SDL_Scancode.SDL_SCANCODE_F24),
            /* Here are keys that don't exist under the US keyboard layout and TK Key enum.
               They don't have scancode, because they usually utilize 'different' keys on US layout.
               Dictionaries below won't work if there is more than one key with same scancode. */
            (InputKey.Colon, (Key)InputKey.Colon, SDL_Keycode.SDLK_COLON, SDL_Scancode.SDL_SCANCODE_UNKNOWN),
            (InputKey.Exclaim, (Key)InputKey.Exclaim, SDL_Keycode.SDLK_EXCLAIM, SDL_Scancode.SDL_SCANCODE_UNKNOWN),
            (InputKey.Dollar, (Key)InputKey.Dollar, SDL_Keycode.SDLK_DOLLAR, SDL_Scancode.SDL_SCANCODE_UNKNOWN),
            (InputKey.Asterisk, (Key)InputKey.Asterisk, SDL_Keycode.SDLK_ASTERISK, SDL_Scancode.SDL_SCANCODE_UNKNOWN),
            (InputKey.RightParen, (Key)InputKey.RightParen, SDL_Keycode.SDLK_RIGHTPAREN, SDL_Scancode.SDL_SCANCODE_UNKNOWN),
            (InputKey.LeftParen, (Key)InputKey.LeftParen, SDL_Keycode.SDLK_LEFTPAREN, SDL_Scancode.SDL_SCANCODE_UNKNOWN),
            (InputKey.Caret, (Key)InputKey.Caret, SDL_Keycode.SDLK_CARET, SDL_Scancode.SDL_SCANCODE_UNKNOWN),
            (InputKey.Less, (Key)InputKey.Less, SDL_Keycode.SDLK_LESS, SDL_Scancode.SDL_SCANCODE_UNKNOWN),
            (InputKey.At, (Key)InputKey.At, SDL_Keycode.SDLK_AT, SDL_Scancode.SDL_SCANCODE_UNKNOWN));

        private static readonly ImmutableDictionary<SDL_Keycode, Key> keycode_mapping = key_mapping.Where(k => k.Item3 != SDL_Keycode.SDLK_UNKNOWN)
                                                                                                   .ToImmutableDictionary(k => k.Item3, v => v.Item2);

        private static readonly ImmutableDictionary<SDL_Scancode, Key> scancode_mapping = key_mapping.Where(k => k.Item4 != SDL_Scancode.SDL_SCANCODE_UNKNOWN)
                                                                                                     .ToImmutableDictionary(k => k.Item4, v => v.Item2);

        private static readonly ImmutableDictionary<InputKey, (SDL_Keycode, SDL_Scancode)> inputkey_mapping = key_mapping.Where(k => k.Item1 != InputKey.None)
                                                                                                                         .ToImmutableDictionary(k => k.Item1, v => (v.Item3, v.Item4));

        public static Key ToKey(this SDL_KeyboardEvent sdlKeyboardEvent)
        {
            Key key = sdlKeyboardEvent.key.ToKey();

            if (key == Key.Unknown && !scancode_mapping.TryGetValue(sdlKeyboardEvent.scancode, out key))
                return Key.Unknown;

            // Apple devices don't have the notion of NumLock (they have a Clear key instead).
            // treat them as if they always have NumLock on (the numpad always performs its primary actions).
            bool numLockOn = sdlKeyboardEvent.mod.HasFlagFast(SDL_Keymod.SDL_KMOD_NUM) || RuntimeInfo.IsApple;

            if (!numLockOn)
            {
                switch (key)
                {
                    case Key.Keypad1:
                        return Key.End;

                    case Key.Keypad2:
                        return Key.Down;

                    case Key.Keypad3:
                        return Key.PageDown;

                    case Key.Keypad4:
                        return Key.Left;

                    case Key.Keypad5:
                        return Key.Clear;

                    case Key.Keypad6:
                        return Key.Right;

                    case Key.Keypad7:
                        return Key.Home;

                    case Key.Keypad8:
                        return Key.Up;

                    case Key.Keypad9:
                        return Key.PageUp;

                    case Key.Keypad0:
                        return Key.PageUp;

                    case Key.KeypadPeriod:
                        return Key.Delete;
                }
            }

            return key;
        }

        public static Key ToKey(this SDL_Keycode sdlKeycode)
        {
            if (keycode_mapping.TryGetValue(sdlKeycode, out var key))
                return key;

            return Key.Unknown;
        }

        /// <summary>
        /// Returns the corresponding <see cref="SDL_Scancode"/> for a given <see cref="InputKey"/>.
        /// </summary>
        /// <param name="inputKey">
        /// Should be a keyboard key.
        /// </param>
        /// <returns>
        /// The corresponding <see cref="SDL_Scancode"/> if the <see cref="InputKey"/> is valid.
        /// <see cref="SDL_Scancode.SDL_SCANCODE_UNKNOWN"/> otherwise.
        /// </returns>
        public static SDL_Scancode ToScancode(this InputKey inputKey)
        {
            if (inputkey_mapping.TryGetValue(inputKey, out var key))
                return key.Item2;

            return SDL_Scancode.SDL_SCANCODE_UNKNOWN;
        }

        public static SDL_Keycode ToKeycode(this InputKey inputKey)
        {
            if (inputkey_mapping.TryGetValue(inputKey, out var key))
                return key.Item1;

            return SDL_Keycode.SDLK_UNKNOWN;
        }

        private static ImmutableDictionary<InputKey, InputKey>? positionalMapping;

        /// <summary>
        /// Generates our own positional mapping on SDL init, because SDL_GetKeyFromScancode is not thread-safe.
        /// Converts all available InputKey and store to a thread-safe ImmutableDictionary.
        /// </summary>
        public static void UpdateKeymap()
        {
            if (positionalMapping != null)
                return;

            if (SDL_WasInit(SDL_InitFlags.SDL_INIT_VIDEO) != SDL_InitFlags.SDL_INIT_VIDEO)
                throw new InvalidOperationException("Initialize SDL before calling this function.");

            ImmutableDictionary<InputKey, InputKey>.Builder builder = ImmutableDictionary.CreateBuilder<InputKey, InputKey>();

            foreach (InputKey key in Enum.GetValues(typeof(InputKey)))
            {
                // Get a plain scancode that corresponds to US layout (Key Q -> Scancode Q)
                SDL_Scancode scancode = key.ToScancode();

                if (scancode == SDL_Scancode.SDL_SCANCODE_UNKNOWN)
                    continue;

                // Convert the scancode to a key that corresponds to current keymap (Scancode Q -> Key A if AZERTY)
                SDL_Keycode keycode = SDL_GetKeyFromScancode(scancode, SDL_Keymod.SDL_KMOD_NONE, true);

                if (keycode == SDL_Keycode.SDLK_UNKNOWN)
                    continue;

                // Finally, convert the converted SDL_Keycode to an InputKey.
                InputKey convertedKey = KeyCombination.FromKey(keycode.ToKey());

                if (convertedKey != InputKey.None && key != convertedKey)
                    builder.Add(key, convertedKey);
            }

            positionalMapping = builder.ToImmutable();
        }

        /// <summary>
        /// This function finds where <paramref name="inputKey"/> is actually located on a keyboard,
        /// and returns the corresponding key that is used in current system keyboard layout.
        /// Basically, this lets you treat <paramref name="inputKey"/> as a scancode.
        /// <see cref="UpdateKeymap"/> must be called before you call this function.
        /// </summary>
        /// <param name="inputKey">A key to convert</param>
        /// <returns>Converted key with current keyboard layout</returns>
        public static InputKey ToPositionalKey(this InputKey inputKey)
        {
            if (positionalMapping == null && !DebugUtils.IsNUnitRunning)
                throw new InvalidOperationException($"{nameof(ToPositionalKey)} must be used after {nameof(UpdateKeymap)} unless in headless mode.");

            if (positionalMapping?.TryGetValue(inputKey, out var mapping) == true)
                return mapping;

            return inputKey;
        }

        public static WindowState ToWindowState(this SDL_WindowFlags windowFlags, bool isFullscreenBorderless)
        {
            // for windows
            if (windowFlags.HasFlagFast(SDL_WindowFlags.SDL_WINDOW_BORDERLESS))
                return WindowState.FullscreenBorderless;

            if (windowFlags.HasFlagFast(SDL_WindowFlags.SDL_WINDOW_MINIMIZED))
                return WindowState.Minimised;

            if (windowFlags.HasFlagFast(SDL_WindowFlags.SDL_WINDOW_FULLSCREEN))
                return isFullscreenBorderless ? WindowState.FullscreenBorderless : WindowState.Fullscreen;

            if (windowFlags.HasFlagFast(SDL_WindowFlags.SDL_WINDOW_MAXIMIZED))
                return WindowState.Maximised;

            return WindowState.Normal;
        }

        public static SDL_WindowFlags ToFlags(this WindowState state)
        {
            switch (state)
            {
                case WindowState.Normal:
                    return 0;

                case WindowState.Fullscreen:
                    return SDL_WindowFlags.SDL_WINDOW_FULLSCREEN;

                case WindowState.Maximised:
                    return SDL_WindowFlags.SDL_WINDOW_MAXIMIZED;

                case WindowState.Minimised:
                    return SDL_WindowFlags.SDL_WINDOW_MINIMIZED;

                case WindowState.FullscreenBorderless:
                    return SDL_WindowFlags.SDL_WINDOW_BORDERLESS;
            }

            return 0;
        }

        public static SDL_WindowFlags ToFlags(this GraphicsSurfaceType surfaceType)
        {
            switch (surfaceType)
            {
                case GraphicsSurfaceType.OpenGL:
                    return SDL_WindowFlags.SDL_WINDOW_OPENGL;

                case GraphicsSurfaceType.Vulkan when !RuntimeInfo.IsApple:
                    return SDL_WindowFlags.SDL_WINDOW_VULKAN;

                case GraphicsSurfaceType.Metal:
                case GraphicsSurfaceType.Vulkan when RuntimeInfo.IsApple:
                    return SDL_WindowFlags.SDL_WINDOW_METAL;
            }

            return 0;
        }

        public static JoystickAxisSource ToJoystickAxisSource(this SDL_GamepadAxis axis)
        {
            switch (axis)
            {
                default:
                case SDL_GamepadAxis.SDL_GAMEPAD_AXIS_INVALID:
                    return 0;

                case SDL_GamepadAxis.SDL_GAMEPAD_AXIS_LEFTX:
                    return JoystickAxisSource.GamePadLeftStickX;

                case SDL_GamepadAxis.SDL_GAMEPAD_AXIS_LEFTY:
                    return JoystickAxisSource.GamePadLeftStickY;

                case SDL_GamepadAxis.SDL_GAMEPAD_AXIS_LEFT_TRIGGER:
                    return JoystickAxisSource.GamePadLeftTrigger;

                case SDL_GamepadAxis.SDL_GAMEPAD_AXIS_RIGHTX:
                    return JoystickAxisSource.GamePadRightStickX;

                case SDL_GamepadAxis.SDL_GAMEPAD_AXIS_RIGHTY:
                    return JoystickAxisSource.GamePadRightStickY;

                case SDL_GamepadAxis.SDL_GAMEPAD_AXIS_RIGHT_TRIGGER:
                    return JoystickAxisSource.GamePadRightTrigger;
            }
        }

        public static JoystickButton ToJoystickButton(this SDL_GamepadButton button)
        {
            switch (button)
            {
                default:
                case SDL_GamepadButton.SDL_GAMEPAD_BUTTON_INVALID:
                    return 0;

                case SDL_GamepadButton.SDL_GAMEPAD_BUTTON_SOUTH:
                    return JoystickButton.GamePadA;

                case SDL_GamepadButton.SDL_GAMEPAD_BUTTON_EAST:
                    return JoystickButton.GamePadB;

                case SDL_GamepadButton.SDL_GAMEPAD_BUTTON_WEST:
                    return JoystickButton.GamePadX;

                case SDL_GamepadButton.SDL_GAMEPAD_BUTTON_NORTH:
                    return JoystickButton.GamePadY;

                case SDL_GamepadButton.SDL_GAMEPAD_BUTTON_BACK:
                    return JoystickButton.GamePadBack;

                case SDL_GamepadButton.SDL_GAMEPAD_BUTTON_GUIDE:
                    return JoystickButton.GamePadGuide;

                case SDL_GamepadButton.SDL_GAMEPAD_BUTTON_START:
                    return JoystickButton.GamePadStart;

                case SDL_GamepadButton.SDL_GAMEPAD_BUTTON_LEFT_STICK:
                    return JoystickButton.GamePadLeftStick;

                case SDL_GamepadButton.SDL_GAMEPAD_BUTTON_RIGHT_STICK:
                    return JoystickButton.GamePadRightStick;

                case SDL_GamepadButton.SDL_GAMEPAD_BUTTON_LEFT_SHOULDER:
                    return JoystickButton.GamePadLeftShoulder;

                case SDL_GamepadButton.SDL_GAMEPAD_BUTTON_RIGHT_SHOULDER:
                    return JoystickButton.GamePadRightShoulder;

                case SDL_GamepadButton.SDL_GAMEPAD_BUTTON_DPAD_UP:
                    return JoystickButton.GamePadDPadUp;

                case SDL_GamepadButton.SDL_GAMEPAD_BUTTON_DPAD_DOWN:
                    return JoystickButton.GamePadDPadDown;

                case SDL_GamepadButton.SDL_GAMEPAD_BUTTON_DPAD_LEFT:
                    return JoystickButton.GamePadDPadLeft;

                case SDL_GamepadButton.SDL_GAMEPAD_BUTTON_DPAD_RIGHT:
                    return JoystickButton.GamePadDPadRight;
            }
        }

        public static SDL_Rect ToSDLRect(this RectangleI rectangle) =>
            new SDL_Rect
            {
                x = rectangle.X,
                y = rectangle.Y,
                h = rectangle.Height,
                w = rectangle.Width,
            };

        public static SDL_TextInputType ToSDLTextInputType(this TextInputType type)
        {
            switch (type)
            {
                default:
                case TextInputType.Text:
                case TextInputType.Code:
                    return SDL_TextInputType.SDL_TEXTINPUT_TYPE_TEXT;

                case TextInputType.Name:
                    return SDL_TextInputType.SDL_TEXTINPUT_TYPE_TEXT_NAME;

                case TextInputType.EmailAddress:
                    return SDL_TextInputType.SDL_TEXTINPUT_TYPE_TEXT_EMAIL;

                case TextInputType.Username:
                    return SDL_TextInputType.SDL_TEXTINPUT_TYPE_TEXT_USERNAME;

                case TextInputType.Number:
                case TextInputType.Decimal:
                    return SDL_TextInputType.SDL_TEXTINPUT_TYPE_NUMBER;

                case TextInputType.Password:
                    return SDL_TextInputType.SDL_TEXTINPUT_TYPE_TEXT_PASSWORD_HIDDEN;

                case TextInputType.NumericalPassword:
                    return SDL_TextInputType.SDL_TEXTINPUT_TYPE_NUMBER_PASSWORD_HIDDEN;
            }
        }

        public static unsafe DisplayMode ToDisplayMode(this SDL_DisplayMode mode, int displayIndex)
        {
            int bpp;
            uint unused;
            SDL_GetMasksForPixelFormat(mode.format, &bpp, &unused, &unused, &unused, &unused).ThrowIfFailed();
            return new DisplayMode(SDL_GetPixelFormatName(mode.format), new Size(mode.w, mode.h), bpp, mode.refresh_rate, displayIndex);
        }

        public static TabletPenDeviceType ToTabletPenDeviceType(this SDL_PenDeviceType type)
        {
            switch (type)
            {
                case SDL_PenDeviceType.SDL_PEN_DEVICE_TYPE_DIRECT:
                    return TabletPenDeviceType.Direct;

                case SDL_PenDeviceType.SDL_PEN_DEVICE_TYPE_INDIRECT:
                    return TabletPenDeviceType.Indirect;

                case SDL_PenDeviceType.SDL_PEN_DEVICE_TYPE_UNKNOWN:
                    return TabletPenDeviceType.Unknown;

                default:
                    Debug.Fail($"Invalid SDL pen device type: {type}");
                    return TabletPenDeviceType.Unknown;
            }
        }

        public static string ReadableName(this SDL_LogCategory category)
        {
            switch (category)
            {
                case SDL_LogCategory.SDL_LOG_CATEGORY_APPLICATION:
                    return "application";

                case SDL_LogCategory.SDL_LOG_CATEGORY_ERROR:
                    return "error";

                case SDL_LogCategory.SDL_LOG_CATEGORY_ASSERT:
                    return "assert";

                case SDL_LogCategory.SDL_LOG_CATEGORY_SYSTEM:
                    return "system";

                case SDL_LogCategory.SDL_LOG_CATEGORY_AUDIO:
                    return "audio";

                case SDL_LogCategory.SDL_LOG_CATEGORY_VIDEO:
                    return "video";

                case SDL_LogCategory.SDL_LOG_CATEGORY_RENDER:
                    return "render";

                case SDL_LogCategory.SDL_LOG_CATEGORY_INPUT:
                    return "input";

                case SDL_LogCategory.SDL_LOG_CATEGORY_TEST:
                    return "test";

                default:
                    return "unknown";
            }
        }

        public static string ReadableName(this SDL_LogPriority priority)
        {
            switch (priority)
            {
                case SDL_LogPriority.SDL_LOG_PRIORITY_VERBOSE:
                    return "verbose";

                case SDL_LogPriority.SDL_LOG_PRIORITY_DEBUG:
                    return "debug";

                case SDL_LogPriority.SDL_LOG_PRIORITY_INFO:
                    return "info";

                case SDL_LogPriority.SDL_LOG_PRIORITY_WARN:
                    return "warn";

                case SDL_LogPriority.SDL_LOG_PRIORITY_ERROR:
                    return "error";

                case SDL_LogPriority.SDL_LOG_PRIORITY_CRITICAL:
                    return "critical";

                default:
                    return "unknown";
            }
        }

        /// <summary>
        /// Gets the readable string for this <see cref="SDL_DisplayMode"/>.
        /// </summary>
        /// <returns>
        /// <c>string</c> in the format of <c>1920x1080@60</c>.
        /// </returns>
        public static string ReadableString(this SDL_DisplayMode mode) => $"{mode.w}x{mode.h}@{mode.refresh_rate}";

        /// <summary>
        /// Gets the SDL error, and then clears it.
        /// </summary>
        public static string? GetAndClearError()
        {
            string? error = SDL_GetError();
            SDL_ClearError();
            return error;
        }

        private static void logError(string? expression)
        {
            Logger.Log($"SDL error: {SDL_GetError()}");
            if (!string.IsNullOrEmpty(expression))
                Logger.Log($"at {expression}");
        }

        public static SDLBool LogErrorIfFailed(this SDLBool returnValue, [CallerArgumentExpression("returnValue")] string? expression = null)
        {
            if (!returnValue)
                logError(expression);

            return returnValue;
        }

        public static void ThrowIfFailed(this SDLBool returnValue, [CallerArgumentExpression("returnValue")] string? expression = null)
        {
            if (!returnValue)
                throw new SDL3Exception(expression);
        }

        public static SDL_PropertiesID ThrowIfFailed(this SDL_PropertiesID returnValue, [CallerArgumentExpression("returnValue")] string? expression = null)
        {
            if (returnValue == 0)
                throw new SDL3Exception(expression);

            return returnValue;
        }

        public static int LogErrorIfFailed(this int returnValue, [CallerArgumentExpression("returnValue")] string? expression = null)
        {
            if (returnValue == -1)
                logError(expression);

            return returnValue;
        }

        public static IntPtr ThrowIfFailed(this IntPtr returnValue, [CallerArgumentExpression("returnValue")] string? expression = null)
        {
            if (returnValue == IntPtr.Zero)
                throw new SDL3Exception(expression);

            return returnValue;
        }

        public static SDLArray<T>? LogErrorIfFailed<T>(this SDLArray<T>? returnValue, [CallerArgumentExpression("returnValue")] string? expression = null)
            where T : unmanaged
        {
            if (returnValue == null)
                logError(expression);

            return returnValue;
        }

        public static string? LogErrorIfFailed(this string? returnValue, [CallerArgumentExpression("returnValue")] string? expression = null)
        {
            if (returnValue == null)
                logError(expression);

            return returnValue;
        }

        public static SDL_DisplayID LogErrorIfFailed(this SDL_DisplayID returnValue, [CallerArgumentExpression("returnValue")] string? expression = null)
        {
            if (returnValue == 0)
                logError(expression);

            return returnValue;
        }

        public static SDL_DisplayID ThrowIfFailed(this SDL_DisplayID returnValue, [CallerArgumentExpression("returnValue")] string? expression = null)
        {
            if (returnValue == 0)
                throw new SDL3Exception(expression);

            return returnValue;
        }

        public static unsafe T* LogErrorIfFailed<T>(T* returnValue, [CallerArgumentExpression("returnValue")] string? expression = null)
            where T : unmanaged
        {
            if (returnValue == null)
                logError(expression);

            return returnValue;
        }
    }
}
