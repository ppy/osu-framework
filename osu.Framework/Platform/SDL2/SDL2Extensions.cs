// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using osu.Framework.Extensions.EnumExtensions;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Input;
using osu.Framework.Input.Bindings;
using osuTK.Input;
using SDL2;

namespace osu.Framework.Platform.SDL2
{
    public static class SDL2Extensions
    {
        private static readonly HashSet<(InputKey, Key, SDL.SDL_Keycode, SDL.SDL_Scancode)> key_mapping = new HashSet<(InputKey, Key, SDL.SDL_Keycode, SDL.SDL_Scancode)>
        {
            (InputKey.Shift, Key.Unknown, SDL.SDL_Keycode.SDLK_UNKNOWN, SDL.SDL_Scancode.SDL_SCANCODE_UNKNOWN),
            (InputKey.Control, Key.Unknown, SDL.SDL_Keycode.SDLK_UNKNOWN, SDL.SDL_Scancode.SDL_SCANCODE_UNKNOWN),
            (InputKey.Alt, Key.Unknown, SDL.SDL_Keycode.SDLK_UNKNOWN, SDL.SDL_Scancode.SDL_SCANCODE_UNKNOWN),
            (InputKey.Super, Key.Unknown, SDL.SDL_Keycode.SDLK_UNKNOWN, SDL.SDL_Scancode.SDL_SCANCODE_UNKNOWN),
            (InputKey.F25, Key.Unknown, SDL.SDL_Keycode.SDLK_UNKNOWN, SDL.SDL_Scancode.SDL_SCANCODE_UNKNOWN),
            (InputKey.F26, Key.Unknown, SDL.SDL_Keycode.SDLK_UNKNOWN, SDL.SDL_Scancode.SDL_SCANCODE_UNKNOWN),
            (InputKey.F27, Key.Unknown, SDL.SDL_Keycode.SDLK_UNKNOWN, SDL.SDL_Scancode.SDL_SCANCODE_UNKNOWN),
            (InputKey.F28, Key.Unknown, SDL.SDL_Keycode.SDLK_UNKNOWN, SDL.SDL_Scancode.SDL_SCANCODE_UNKNOWN),
            (InputKey.F29, Key.Unknown, SDL.SDL_Keycode.SDLK_UNKNOWN, SDL.SDL_Scancode.SDL_SCANCODE_UNKNOWN),
            (InputKey.F30, Key.Unknown, SDL.SDL_Keycode.SDLK_UNKNOWN, SDL.SDL_Scancode.SDL_SCANCODE_UNKNOWN),
            (InputKey.F31, Key.Unknown, SDL.SDL_Keycode.SDLK_UNKNOWN, SDL.SDL_Scancode.SDL_SCANCODE_UNKNOWN),
            (InputKey.F32, Key.Unknown, SDL.SDL_Keycode.SDLK_UNKNOWN, SDL.SDL_Scancode.SDL_SCANCODE_UNKNOWN),
            (InputKey.F33, Key.Unknown, SDL.SDL_Keycode.SDLK_UNKNOWN, SDL.SDL_Scancode.SDL_SCANCODE_UNKNOWN),
            (InputKey.F34, Key.Unknown, SDL.SDL_Keycode.SDLK_UNKNOWN, SDL.SDL_Scancode.SDL_SCANCODE_UNKNOWN),
            (InputKey.F35, Key.Unknown, SDL.SDL_Keycode.SDLK_UNKNOWN, SDL.SDL_Scancode.SDL_SCANCODE_UNKNOWN),
            (InputKey.None, Key.Unknown, SDL.SDL_Keycode.SDLK_UNKNOWN, SDL.SDL_Scancode.SDL_SCANCODE_UNKNOWN),
            (InputKey.None, Key.Comma, SDL.SDL_Keycode.SDLK_KP_COMMA, SDL.SDL_Scancode.SDL_SCANCODE_KP_COMMA),
            (InputKey.None, Key.Tab, SDL.SDL_Keycode.SDLK_KP_TAB, SDL.SDL_Scancode.SDL_SCANCODE_KP_TAB),
            (InputKey.None, Key.BackSpace, SDL.SDL_Keycode.SDLK_KP_BACKSPACE, SDL.SDL_Scancode.SDL_SCANCODE_KP_BACKSPACE),
            (InputKey.None, Key.A, SDL.SDL_Keycode.SDLK_KP_A, SDL.SDL_Scancode.SDL_SCANCODE_KP_A),
            (InputKey.None, Key.B, SDL.SDL_Keycode.SDLK_KP_B, SDL.SDL_Scancode.SDL_SCANCODE_KP_B),
            (InputKey.None, Key.C, SDL.SDL_Keycode.SDLK_KP_C, SDL.SDL_Scancode.SDL_SCANCODE_KP_C),
            (InputKey.None, Key.D, SDL.SDL_Keycode.SDLK_KP_D, SDL.SDL_Scancode.SDL_SCANCODE_KP_D),
            (InputKey.None, Key.E, SDL.SDL_Keycode.SDLK_KP_E, SDL.SDL_Scancode.SDL_SCANCODE_KP_E),
            (InputKey.None, Key.F, SDL.SDL_Keycode.SDLK_KP_F, SDL.SDL_Scancode.SDL_SCANCODE_KP_F),
            (InputKey.None, Key.Space, SDL.SDL_Keycode.SDLK_KP_SPACE, SDL.SDL_Scancode.SDL_SCANCODE_KP_SPACE),
            (InputKey.None, Key.Clear, SDL.SDL_Keycode.SDLK_KP_CLEAR, SDL.SDL_Scancode.SDL_SCANCODE_KP_CLEAR),
            (InputKey.Enter, Key.Enter, SDL.SDL_Keycode.SDLK_RETURN, SDL.SDL_Scancode.SDL_SCANCODE_RETURN),
            (InputKey.Escape, Key.Escape, SDL.SDL_Keycode.SDLK_ESCAPE, SDL.SDL_Scancode.SDL_SCANCODE_ESCAPE),
            (InputKey.BackSpace, Key.BackSpace, SDL.SDL_Keycode.SDLK_BACKSPACE, SDL.SDL_Scancode.SDL_SCANCODE_BACKSPACE),
            (InputKey.Tab, Key.Tab, SDL.SDL_Keycode.SDLK_TAB, SDL.SDL_Scancode.SDL_SCANCODE_TAB),
            (InputKey.Space, Key.Space, SDL.SDL_Keycode.SDLK_SPACE, SDL.SDL_Scancode.SDL_SCANCODE_SPACE),
            (InputKey.Quote, Key.Quote, SDL.SDL_Keycode.SDLK_UNKNOWN, SDL.SDL_Scancode.SDL_SCANCODE_APOSTROPHE),
            (InputKey.Comma, Key.Comma, SDL.SDL_Keycode.SDLK_COMMA, SDL.SDL_Scancode.SDL_SCANCODE_COMMA),
            (InputKey.Minus, Key.Minus, SDL.SDL_Keycode.SDLK_UNKNOWN, SDL.SDL_Scancode.SDL_SCANCODE_MINUS),
            (InputKey.Period, Key.Period, SDL.SDL_Keycode.SDLK_UNKNOWN, SDL.SDL_Scancode.SDL_SCANCODE_PERIOD),
            (InputKey.Slash, Key.Slash, SDL.SDL_Keycode.SDLK_UNKNOWN, SDL.SDL_Scancode.SDL_SCANCODE_SLASH),
            (InputKey.Number0, Key.Number0, SDL.SDL_Keycode.SDLK_0, SDL.SDL_Scancode.SDL_SCANCODE_0),
            (InputKey.Number1, Key.Number1, SDL.SDL_Keycode.SDLK_1, SDL.SDL_Scancode.SDL_SCANCODE_1),
            (InputKey.Number2, Key.Number2, SDL.SDL_Keycode.SDLK_2, SDL.SDL_Scancode.SDL_SCANCODE_2),
            (InputKey.Number3, Key.Number3, SDL.SDL_Keycode.SDLK_3, SDL.SDL_Scancode.SDL_SCANCODE_3),
            (InputKey.Number4, Key.Number4, SDL.SDL_Keycode.SDLK_4, SDL.SDL_Scancode.SDL_SCANCODE_4),
            (InputKey.Number5, Key.Number5, SDL.SDL_Keycode.SDLK_5, SDL.SDL_Scancode.SDL_SCANCODE_5),
            (InputKey.Number6, Key.Number6, SDL.SDL_Keycode.SDLK_6, SDL.SDL_Scancode.SDL_SCANCODE_6),
            (InputKey.Number7, Key.Number7, SDL.SDL_Keycode.SDLK_7, SDL.SDL_Scancode.SDL_SCANCODE_7),
            (InputKey.Number8, Key.Number8, SDL.SDL_Keycode.SDLK_8, SDL.SDL_Scancode.SDL_SCANCODE_8),
            (InputKey.Number9, Key.Number9, SDL.SDL_Keycode.SDLK_9, SDL.SDL_Scancode.SDL_SCANCODE_9),
            (InputKey.Semicolon, Key.Semicolon, SDL.SDL_Keycode.SDLK_UNKNOWN, SDL.SDL_Scancode.SDL_SCANCODE_SEMICOLON),
            (InputKey.Plus, Key.Plus, SDL.SDL_Keycode.SDLK_EQUALS, SDL.SDL_Scancode.SDL_SCANCODE_EQUALS),
            (InputKey.BracketLeft, Key.BracketLeft, SDL.SDL_Keycode.SDLK_UNKNOWN, SDL.SDL_Scancode.SDL_SCANCODE_LEFTBRACKET),
            (InputKey.BackSlash, Key.BackSlash, SDL.SDL_Keycode.SDLK_UNKNOWN, SDL.SDL_Scancode.SDL_SCANCODE_BACKSLASH),
            (InputKey.BracketRight, Key.BracketRight, SDL.SDL_Keycode.SDLK_UNKNOWN, SDL.SDL_Scancode.SDL_SCANCODE_RIGHTBRACKET),
            (InputKey.Tilde, Key.Tilde, SDL.SDL_Keycode.SDLK_UNKNOWN, SDL.SDL_Scancode.SDL_SCANCODE_GRAVE),
            (InputKey.A, Key.A, SDL.SDL_Keycode.SDLK_a, SDL.SDL_Scancode.SDL_SCANCODE_A),
            (InputKey.B, Key.B, SDL.SDL_Keycode.SDLK_b, SDL.SDL_Scancode.SDL_SCANCODE_B),
            (InputKey.C, Key.C, SDL.SDL_Keycode.SDLK_c, SDL.SDL_Scancode.SDL_SCANCODE_C),
            (InputKey.D, Key.D, SDL.SDL_Keycode.SDLK_d, SDL.SDL_Scancode.SDL_SCANCODE_D),
            (InputKey.E, Key.E, SDL.SDL_Keycode.SDLK_e, SDL.SDL_Scancode.SDL_SCANCODE_E),
            (InputKey.F, Key.F, SDL.SDL_Keycode.SDLK_f, SDL.SDL_Scancode.SDL_SCANCODE_F),
            (InputKey.G, Key.G, SDL.SDL_Keycode.SDLK_g, SDL.SDL_Scancode.SDL_SCANCODE_G),
            (InputKey.H, Key.H, SDL.SDL_Keycode.SDLK_h, SDL.SDL_Scancode.SDL_SCANCODE_H),
            (InputKey.I, Key.I, SDL.SDL_Keycode.SDLK_i, SDL.SDL_Scancode.SDL_SCANCODE_I),
            (InputKey.J, Key.J, SDL.SDL_Keycode.SDLK_j, SDL.SDL_Scancode.SDL_SCANCODE_J),
            (InputKey.K, Key.K, SDL.SDL_Keycode.SDLK_k, SDL.SDL_Scancode.SDL_SCANCODE_K),
            (InputKey.L, Key.L, SDL.SDL_Keycode.SDLK_l, SDL.SDL_Scancode.SDL_SCANCODE_L),
            (InputKey.M, Key.M, SDL.SDL_Keycode.SDLK_m, SDL.SDL_Scancode.SDL_SCANCODE_M),
            (InputKey.N, Key.N, SDL.SDL_Keycode.SDLK_n, SDL.SDL_Scancode.SDL_SCANCODE_N),
            (InputKey.O, Key.O, SDL.SDL_Keycode.SDLK_o, SDL.SDL_Scancode.SDL_SCANCODE_O),
            (InputKey.P, Key.P, SDL.SDL_Keycode.SDLK_p, SDL.SDL_Scancode.SDL_SCANCODE_P),
            (InputKey.Q, Key.Q, SDL.SDL_Keycode.SDLK_q, SDL.SDL_Scancode.SDL_SCANCODE_Q),
            (InputKey.R, Key.R, SDL.SDL_Keycode.SDLK_r, SDL.SDL_Scancode.SDL_SCANCODE_R),
            (InputKey.S, Key.S, SDL.SDL_Keycode.SDLK_s, SDL.SDL_Scancode.SDL_SCANCODE_S),
            (InputKey.T, Key.T, SDL.SDL_Keycode.SDLK_t, SDL.SDL_Scancode.SDL_SCANCODE_T),
            (InputKey.U, Key.U, SDL.SDL_Keycode.SDLK_u, SDL.SDL_Scancode.SDL_SCANCODE_U),
            (InputKey.V, Key.V, SDL.SDL_Keycode.SDLK_v, SDL.SDL_Scancode.SDL_SCANCODE_V),
            (InputKey.W, Key.W, SDL.SDL_Keycode.SDLK_w, SDL.SDL_Scancode.SDL_SCANCODE_W),
            (InputKey.X, Key.X, SDL.SDL_Keycode.SDLK_x, SDL.SDL_Scancode.SDL_SCANCODE_X),
            (InputKey.Y, Key.Y, SDL.SDL_Keycode.SDLK_y, SDL.SDL_Scancode.SDL_SCANCODE_Y),
            (InputKey.Z, Key.Z, SDL.SDL_Keycode.SDLK_z, SDL.SDL_Scancode.SDL_SCANCODE_Z),
            (InputKey.CapsLock, Key.CapsLock, SDL.SDL_Keycode.SDLK_CAPSLOCK, SDL.SDL_Scancode.SDL_SCANCODE_CAPSLOCK),
            (InputKey.F1, Key.F1, SDL.SDL_Keycode.SDLK_F1, SDL.SDL_Scancode.SDL_SCANCODE_F1),
            (InputKey.F2, Key.F2, SDL.SDL_Keycode.SDLK_F2, SDL.SDL_Scancode.SDL_SCANCODE_F2),
            (InputKey.F3, Key.F3, SDL.SDL_Keycode.SDLK_F3, SDL.SDL_Scancode.SDL_SCANCODE_F3),
            (InputKey.F4, Key.F4, SDL.SDL_Keycode.SDLK_F4, SDL.SDL_Scancode.SDL_SCANCODE_F4),
            (InputKey.F5, Key.F5, SDL.SDL_Keycode.SDLK_F5, SDL.SDL_Scancode.SDL_SCANCODE_F5),
            (InputKey.F6, Key.F6, SDL.SDL_Keycode.SDLK_F6, SDL.SDL_Scancode.SDL_SCANCODE_F6),
            (InputKey.F7, Key.F7, SDL.SDL_Keycode.SDLK_F7, SDL.SDL_Scancode.SDL_SCANCODE_F7),
            (InputKey.F8, Key.F8, SDL.SDL_Keycode.SDLK_F8, SDL.SDL_Scancode.SDL_SCANCODE_F8),
            (InputKey.F9, Key.F9, SDL.SDL_Keycode.SDLK_F9, SDL.SDL_Scancode.SDL_SCANCODE_F9),
            (InputKey.F10, Key.F10, SDL.SDL_Keycode.SDLK_F10, SDL.SDL_Scancode.SDL_SCANCODE_F10),
            (InputKey.F11, Key.F11, SDL.SDL_Keycode.SDLK_F11, SDL.SDL_Scancode.SDL_SCANCODE_F11),
            (InputKey.F12, Key.F12, SDL.SDL_Keycode.SDLK_F12, SDL.SDL_Scancode.SDL_SCANCODE_F12),
            (InputKey.PrintScreen, Key.PrintScreen, SDL.SDL_Keycode.SDLK_PRINTSCREEN, SDL.SDL_Scancode.SDL_SCANCODE_PRINTSCREEN),
            (InputKey.ScrollLock, Key.ScrollLock, SDL.SDL_Keycode.SDLK_SCROLLLOCK, SDL.SDL_Scancode.SDL_SCANCODE_SCROLLLOCK),
            (InputKey.Pause, Key.Pause, SDL.SDL_Keycode.SDLK_PAUSE, SDL.SDL_Scancode.SDL_SCANCODE_PAUSE),
            (InputKey.Insert, Key.Insert, SDL.SDL_Keycode.SDLK_INSERT, SDL.SDL_Scancode.SDL_SCANCODE_INSERT),
            (InputKey.Home, Key.Home, SDL.SDL_Keycode.SDLK_HOME, SDL.SDL_Scancode.SDL_SCANCODE_HOME),
            (InputKey.PageUp, Key.PageUp, SDL.SDL_Keycode.SDLK_PAGEUP, SDL.SDL_Scancode.SDL_SCANCODE_PAGEUP),
            (InputKey.Delete, Key.Delete, SDL.SDL_Keycode.SDLK_DELETE, SDL.SDL_Scancode.SDL_SCANCODE_DELETE),
            (InputKey.End, Key.End, SDL.SDL_Keycode.SDLK_END, SDL.SDL_Scancode.SDL_SCANCODE_END),
            (InputKey.PageDown, Key.PageDown, SDL.SDL_Keycode.SDLK_PAGEDOWN, SDL.SDL_Scancode.SDL_SCANCODE_PAGEDOWN),
            (InputKey.Right, Key.Right, SDL.SDL_Keycode.SDLK_RIGHT, SDL.SDL_Scancode.SDL_SCANCODE_RIGHT),
            (InputKey.Left, Key.Left, SDL.SDL_Keycode.SDLK_LEFT, SDL.SDL_Scancode.SDL_SCANCODE_LEFT),
            (InputKey.Down, Key.Down, SDL.SDL_Keycode.SDLK_DOWN, SDL.SDL_Scancode.SDL_SCANCODE_DOWN),
            (InputKey.Up, Key.Up, SDL.SDL_Keycode.SDLK_UP, SDL.SDL_Scancode.SDL_SCANCODE_UP),
            (InputKey.NumLock, Key.NumLock, SDL.SDL_Keycode.SDLK_NUMLOCKCLEAR, SDL.SDL_Scancode.SDL_SCANCODE_NUMLOCKCLEAR),
            (InputKey.KeypadDivide, Key.KeypadDivide, SDL.SDL_Keycode.SDLK_KP_DIVIDE, SDL.SDL_Scancode.SDL_SCANCODE_KP_DIVIDE),
            (InputKey.KeypadMultiply, Key.KeypadMultiply, SDL.SDL_Keycode.SDLK_KP_MULTIPLY, SDL.SDL_Scancode.SDL_SCANCODE_KP_MULTIPLY),
            (InputKey.KeypadMinus, Key.KeypadMinus, SDL.SDL_Keycode.SDLK_KP_MINUS, SDL.SDL_Scancode.SDL_SCANCODE_KP_MINUS),
            (InputKey.KeypadPlus, Key.KeypadPlus, SDL.SDL_Keycode.SDLK_KP_PLUS, SDL.SDL_Scancode.SDL_SCANCODE_KP_PLUS),
            (InputKey.KeypadEnter, Key.KeypadEnter, SDL.SDL_Keycode.SDLK_KP_ENTER, SDL.SDL_Scancode.SDL_SCANCODE_KP_ENTER),
            (InputKey.Keypad1, Key.Keypad1, SDL.SDL_Keycode.SDLK_KP_1, SDL.SDL_Scancode.SDL_SCANCODE_KP_1),
            (InputKey.Keypad2, Key.Keypad2, SDL.SDL_Keycode.SDLK_KP_2, SDL.SDL_Scancode.SDL_SCANCODE_KP_2),
            (InputKey.Keypad3, Key.Keypad3, SDL.SDL_Keycode.SDLK_KP_3, SDL.SDL_Scancode.SDL_SCANCODE_KP_3),
            (InputKey.Keypad4, Key.Keypad4, SDL.SDL_Keycode.SDLK_KP_4, SDL.SDL_Scancode.SDL_SCANCODE_KP_4),
            (InputKey.Keypad5, Key.Keypad5, SDL.SDL_Keycode.SDLK_KP_5, SDL.SDL_Scancode.SDL_SCANCODE_KP_5),
            (InputKey.Keypad6, Key.Keypad6, SDL.SDL_Keycode.SDLK_KP_6, SDL.SDL_Scancode.SDL_SCANCODE_KP_6),
            (InputKey.Keypad7, Key.Keypad7, SDL.SDL_Keycode.SDLK_KP_7, SDL.SDL_Scancode.SDL_SCANCODE_KP_7),
            (InputKey.Keypad8, Key.Keypad8, SDL.SDL_Keycode.SDLK_KP_8, SDL.SDL_Scancode.SDL_SCANCODE_KP_8),
            (InputKey.Keypad9, Key.Keypad9, SDL.SDL_Keycode.SDLK_KP_9, SDL.SDL_Scancode.SDL_SCANCODE_KP_9),
            (InputKey.Keypad0, Key.Keypad0, SDL.SDL_Keycode.SDLK_KP_0, SDL.SDL_Scancode.SDL_SCANCODE_KP_0),
            (InputKey.KeypadPeriod, Key.KeypadPeriod, SDL.SDL_Keycode.SDLK_KP_PERIOD, SDL.SDL_Scancode.SDL_SCANCODE_KP_PERIOD),
            (InputKey.NonUSBackSlash, Key.NonUSBackSlash, SDL.SDL_Keycode.SDLK_UNKNOWN, SDL.SDL_Scancode.SDL_SCANCODE_NONUSBACKSLASH),
            (InputKey.F13, Key.F13, SDL.SDL_Keycode.SDLK_F13, SDL.SDL_Scancode.SDL_SCANCODE_F13),
            (InputKey.F14, Key.F14, SDL.SDL_Keycode.SDLK_F14, SDL.SDL_Scancode.SDL_SCANCODE_F14),
            (InputKey.F15, Key.F15, SDL.SDL_Keycode.SDLK_F15, SDL.SDL_Scancode.SDL_SCANCODE_F15),
            (InputKey.F16, Key.F16, SDL.SDL_Keycode.SDLK_F16, SDL.SDL_Scancode.SDL_SCANCODE_F16),
            (InputKey.F17, Key.F17, SDL.SDL_Keycode.SDLK_F17, SDL.SDL_Scancode.SDL_SCANCODE_F17),
            (InputKey.F18, Key.F18, SDL.SDL_Keycode.SDLK_F18, SDL.SDL_Scancode.SDL_SCANCODE_F18),
            (InputKey.F19, Key.F19, SDL.SDL_Keycode.SDLK_F19, SDL.SDL_Scancode.SDL_SCANCODE_F19),
            (InputKey.F20, Key.F20, SDL.SDL_Keycode.SDLK_F20, SDL.SDL_Scancode.SDL_SCANCODE_F20),
            (InputKey.F21, Key.F21, SDL.SDL_Keycode.SDLK_F21, SDL.SDL_Scancode.SDL_SCANCODE_F21),
            (InputKey.F22, Key.F22, SDL.SDL_Keycode.SDLK_F22, SDL.SDL_Scancode.SDL_SCANCODE_F22),
            (InputKey.F23, Key.F23, SDL.SDL_Keycode.SDLK_F23, SDL.SDL_Scancode.SDL_SCANCODE_F23),
            (InputKey.F24, Key.F24, SDL.SDL_Keycode.SDLK_F24, SDL.SDL_Scancode.SDL_SCANCODE_F24),
            (InputKey.Menu, Key.Menu, SDL.SDL_Keycode.SDLK_MENU, SDL.SDL_Scancode.SDL_SCANCODE_MENU),
            (InputKey.None, Key.Menu, SDL.SDL_Keycode.SDLK_APPLICATION, SDL.SDL_Scancode.SDL_SCANCODE_APPLICATION),
            (InputKey.None, Key.Stop, SDL.SDL_Keycode.SDLK_STOP, SDL.SDL_Scancode.SDL_SCANCODE_STOP),
            (InputKey.Mute, Key.Mute, SDL.SDL_Keycode.SDLK_MUTE, SDL.SDL_Scancode.SDL_SCANCODE_MUTE),
            (InputKey.VolumeUp, Key.VolumeUp, SDL.SDL_Keycode.SDLK_VOLUMEUP, SDL.SDL_Scancode.SDL_SCANCODE_VOLUMEUP),
            (InputKey.VolumeDown, Key.VolumeDown, SDL.SDL_Keycode.SDLK_VOLUMEDOWN, SDL.SDL_Scancode.SDL_SCANCODE_VOLUMEDOWN),
            (InputKey.None, Key.Clear, SDL.SDL_Keycode.SDLK_CLEAR, SDL.SDL_Scancode.SDL_SCANCODE_CLEAR),
            (InputKey.None, Key.KeypadDecimal, SDL.SDL_Keycode.SDLK_DECIMALSEPARATOR, SDL.SDL_Scancode.SDL_SCANCODE_DECIMALSEPARATOR),
            (InputKey.LControl, Key.ControlLeft, SDL.SDL_Keycode.SDLK_LCTRL, SDL.SDL_Scancode.SDL_SCANCODE_LCTRL),
            (InputKey.LShift, Key.ShiftLeft, SDL.SDL_Keycode.SDLK_LSHIFT, SDL.SDL_Scancode.SDL_SCANCODE_LSHIFT),
            (InputKey.LAlt, Key.AltLeft, SDL.SDL_Keycode.SDLK_LALT, SDL.SDL_Scancode.SDL_SCANCODE_LALT),
            (InputKey.LSuper, Key.WinLeft, SDL.SDL_Keycode.SDLK_LGUI, SDL.SDL_Scancode.SDL_SCANCODE_LGUI),
            (InputKey.RControl, Key.ControlRight, SDL.SDL_Keycode.SDLK_RCTRL, SDL.SDL_Scancode.SDL_SCANCODE_RCTRL),
            (InputKey.RShift, Key.ShiftRight, SDL.SDL_Keycode.SDLK_RSHIFT, SDL.SDL_Scancode.SDL_SCANCODE_RSHIFT),
            (InputKey.RAlt, Key.AltRight, SDL.SDL_Keycode.SDLK_RALT, SDL.SDL_Scancode.SDL_SCANCODE_RALT),
            (InputKey.RSuper, Key.WinRight, SDL.SDL_Keycode.SDLK_RGUI, SDL.SDL_Scancode.SDL_SCANCODE_RGUI),
            (InputKey.TrackNext, Key.TrackNext, SDL.SDL_Keycode.SDLK_AUDIONEXT, SDL.SDL_Scancode.SDL_SCANCODE_AUDIONEXT),
            (InputKey.TrackPrevious, Key.TrackPrevious, SDL.SDL_Keycode.SDLK_AUDIOPREV, SDL.SDL_Scancode.SDL_SCANCODE_AUDIOPREV),
            (InputKey.Stop, Key.Stop, SDL.SDL_Keycode.SDLK_AUDIOSTOP, SDL.SDL_Scancode.SDL_SCANCODE_AUDIOSTOP),
            (InputKey.PlayPause, Key.PlayPause, SDL.SDL_Keycode.SDLK_AUDIOPLAY, SDL.SDL_Scancode.SDL_SCANCODE_AUDIOPLAY),
            (InputKey.None, Key.PlayPause, SDL.SDL_Keycode.SDLK_AUDIOMUTE, SDL.SDL_Scancode.SDL_SCANCODE_AUDIOMUTE),
            (InputKey.Sleep, Key.Sleep, SDL.SDL_Keycode.SDLK_SLEEP, SDL.SDL_Scancode.SDL_SCANCODE_SLEEP)
        };

        private static readonly Dictionary<SDL.SDL_Keycode, Key> keycode_mapping = key_mapping.Where(k => k.Item3 != SDL.SDL_Keycode.SDLK_UNKNOWN)
                                                                                              .ToDictionary(k => k.Item3, v => v.Item2);

        private static readonly Dictionary<SDL.SDL_Scancode, Key> scancode_mapping = key_mapping.Where(k => k.Item4 != SDL.SDL_Scancode.SDL_SCANCODE_UNKNOWN)
                                                                                                .ToDictionary(k => k.Item4, v => v.Item2);

        private static readonly Dictionary<InputKey, (SDL.SDL_Keycode, SDL.SDL_Scancode)> inputkey_mapping = key_mapping.Where(k => k.Item1 != InputKey.None)
                                                                                                                        .ToDictionary(k => k.Item1, v => (v.Item3, v.Item4));

        private static Key checkNumLock(this Key key, SDL.SDL_Keysym sdlKeysym)
        {
            // Apple devices don't have the notion of NumLock (they have a Clear key instead).
            // treat them as if they always have NumLock on (the numpad always performs its primary actions).
            bool numLockOn = sdlKeysym.mod.HasFlagFast(SDL.SDL_Keymod.KMOD_NUM) || RuntimeInfo.IsApple;

            if (!numLockOn)
            {
                switch (key)
                {
                    default:
                        return key;

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

        public static Key ToKey(this SDL.SDL_Keysym sdlKeysym)
        {
            if (keycode_mapping.TryGetValue(sdlKeysym.sym, out var key))
                return key.checkNumLock(sdlKeysym);

            if (scancode_mapping.TryGetValue(sdlKeysym.scancode, out key))
                return key.checkNumLock(sdlKeysym);

            return Key.Unknown;
        }

        /// <summary>
        /// Returns the corresponding <see cref="SDL.SDL_Keycode"/> for a given <see cref="InputKey"/>.
        /// </summary>
        /// <param name="inputKey">
        /// Should be a keyboard key.
        /// </param>
        /// <returns>
        /// The corresponding <see cref="SDL.SDL_Keycode"/> if the <see cref="InputKey"/> is valid.
        /// <see cref="SDL.SDL_Keycode.SDLK_UNKNOWN"/> otherwise.
        /// </returns>
        public static SDL.SDL_Keycode ToKeycode(this InputKey inputKey)
        {
            if (inputkey_mapping.TryGetValue(inputKey, out var key))
            {
                if (key.Item1 != SDL.SDL_Keycode.SDLK_UNKNOWN)
                    return key.Item1;
                else
                    return SDL.SDL_GetKeyFromScancode(inputKey.ToScancode());
            }

            return SDL.SDL_Keycode.SDLK_UNKNOWN;
        }

        /// <summary>
        /// Returns the corresponding <see cref="SDL.SDL_Scancode"/> for a given <see cref="InputKey"/>.
        /// </summary>
        /// <param name="inputKey">
        /// Should be a keyboard key.
        /// </param>
        /// <returns>
        /// The corresponding <see cref="SDL.SDL_Scancode"/> if the <see cref="InputKey"/> is valid.
        /// <see cref="SDL.SDL_Scancode.SDL_SCANCODE_UNKNOWN"/> otherwise.
        /// </returns>
        public static SDL.SDL_Scancode ToScancode(this InputKey inputKey)
        {
            if (inputkey_mapping.TryGetValue(inputKey, out var key))
                return key.Item2;

            return SDL.SDL_Scancode.SDL_SCANCODE_UNKNOWN;
        }

        public static WindowState ToWindowState(this SDL.SDL_WindowFlags windowFlags)
        {
            if (windowFlags.HasFlagFast(SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN_DESKTOP) ||
                windowFlags.HasFlagFast(SDL.SDL_WindowFlags.SDL_WINDOW_BORDERLESS))
                return WindowState.FullscreenBorderless;

            if (windowFlags.HasFlagFast(SDL.SDL_WindowFlags.SDL_WINDOW_MINIMIZED))
                return WindowState.Minimised;

            if (windowFlags.HasFlagFast(SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN))
                return WindowState.Fullscreen;

            if (windowFlags.HasFlagFast(SDL.SDL_WindowFlags.SDL_WINDOW_MAXIMIZED))
                return WindowState.Maximised;

            return WindowState.Normal;
        }

        public static SDL.SDL_WindowFlags ToFlags(this WindowState state)
        {
            switch (state)
            {
                case WindowState.Normal:
                    return 0;

                case WindowState.Fullscreen:
                    return SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN;

                case WindowState.Maximised:
                    return SDL.SDL_WindowFlags.SDL_WINDOW_MAXIMIZED;

                case WindowState.Minimised:
                    return SDL.SDL_WindowFlags.SDL_WINDOW_MINIMIZED;

                case WindowState.FullscreenBorderless:
                    return SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN_DESKTOP;
            }

            return 0;
        }

        public static SDL.SDL_WindowFlags ToFlags(this GraphicsSurfaceType surfaceType)
        {
            switch (surfaceType)
            {
                case GraphicsSurfaceType.OpenGL:
                    return SDL.SDL_WindowFlags.SDL_WINDOW_OPENGL;

                case GraphicsSurfaceType.Vulkan when !RuntimeInfo.IsApple:
                    return SDL.SDL_WindowFlags.SDL_WINDOW_VULKAN;

                case GraphicsSurfaceType.Metal:
                case GraphicsSurfaceType.Vulkan when RuntimeInfo.IsApple:
                    return SDL.SDL_WindowFlags.SDL_WINDOW_METAL;
            }

            return 0;
        }

        public static JoystickAxisSource ToJoystickAxisSource(this SDL.SDL_GameControllerAxis axis)
        {
            switch (axis)
            {
                default:
                case SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_INVALID:
                    return 0;

                case SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTX:
                    return JoystickAxisSource.GamePadLeftStickX;

                case SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTY:
                    return JoystickAxisSource.GamePadLeftStickY;

                case SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_TRIGGERLEFT:
                    return JoystickAxisSource.GamePadLeftTrigger;

                case SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_RIGHTX:
                    return JoystickAxisSource.GamePadRightStickX;

                case SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_RIGHTY:
                    return JoystickAxisSource.GamePadRightStickY;

                case SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_TRIGGERRIGHT:
                    return JoystickAxisSource.GamePadRightTrigger;
            }
        }

        public static JoystickButton ToJoystickButton(this SDL.SDL_GameControllerButton button)
        {
            switch (button)
            {
                default:
                case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_INVALID:
                    return 0;

                case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_A:
                    return JoystickButton.GamePadA;

                case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_B:
                    return JoystickButton.GamePadB;

                case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_X:
                    return JoystickButton.GamePadX;

                case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_Y:
                    return JoystickButton.GamePadY;

                case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_BACK:
                    return JoystickButton.GamePadBack;

                case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_GUIDE:
                    return JoystickButton.GamePadGuide;

                case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_START:
                    return JoystickButton.GamePadStart;

                case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_LEFTSTICK:
                    return JoystickButton.GamePadLeftStick;

                case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_RIGHTSTICK:
                    return JoystickButton.GamePadRightStick;

                case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_LEFTSHOULDER:
                    return JoystickButton.GamePadLeftShoulder;

                case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_RIGHTSHOULDER:
                    return JoystickButton.GamePadRightShoulder;

                case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_UP:
                    return JoystickButton.GamePadDPadUp;

                case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_DOWN:
                    return JoystickButton.GamePadDPadDown;

                case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_LEFT:
                    return JoystickButton.GamePadDPadLeft;

                case SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_RIGHT:
                    return JoystickButton.GamePadDPadRight;
            }
        }

        public static SDL.SDL_Rect ToSDLRect(this RectangleI rectangle) =>
            new SDL.SDL_Rect
            {
                x = rectangle.X,
                y = rectangle.Y,
                h = rectangle.Height,
                w = rectangle.Width,
            };

        /// <summary>
        /// Converts a UTF-8 byte pointer to a string.
        /// </summary>
        /// <remarks>Most commonly used with SDL text events.</remarks>
        /// <param name="bytePointer">Pointer to UTF-8 encoded byte array.</param>
        /// <param name="str">The resulting string</param>
        /// <returns><c>true</c> if the <paramref name="bytePointer"/> was successfully converted to a string.</returns>
        public static unsafe bool TryGetStringFromBytePointer(byte* bytePointer, out string str)
        {
            IntPtr ptr = new IntPtr(bytePointer);

            if (ptr == IntPtr.Zero)
            {
                str = null;
                return false;
            }

            str = Marshal.PtrToStringUTF8(ptr) ?? string.Empty;
            return true;
        }

        public static DisplayMode ToDisplayMode(this SDL.SDL_DisplayMode mode, int displayIndex)
        {
            SDL.SDL_PixelFormatEnumToMasks(mode.format, out int bpp, out _, out _, out _, out _);
            return new DisplayMode(SDL.SDL_GetPixelFormatName(mode.format), new Size(mode.w, mode.h), bpp, mode.refresh_rate, displayIndex);
        }

        public static string ReadableName(this SDL.SDL_LogCategory category)
        {
            switch (category)
            {
                case SDL.SDL_LogCategory.SDL_LOG_CATEGORY_APPLICATION:
                    return "application";

                case SDL.SDL_LogCategory.SDL_LOG_CATEGORY_ERROR:
                    return "error";

                case SDL.SDL_LogCategory.SDL_LOG_CATEGORY_ASSERT:
                    return "assert";

                case SDL.SDL_LogCategory.SDL_LOG_CATEGORY_SYSTEM:
                    return "system";

                case SDL.SDL_LogCategory.SDL_LOG_CATEGORY_AUDIO:
                    return "audio";

                case SDL.SDL_LogCategory.SDL_LOG_CATEGORY_VIDEO:
                    return "video";

                case SDL.SDL_LogCategory.SDL_LOG_CATEGORY_RENDER:
                    return "render";

                case SDL.SDL_LogCategory.SDL_LOG_CATEGORY_INPUT:
                    return "input";

                case SDL.SDL_LogCategory.SDL_LOG_CATEGORY_TEST:
                    return "test";

                default:
                    return "unknown";
            }
        }

        public static string ReadableName(this SDL.SDL_LogPriority priority)
        {
            switch (priority)
            {
                case SDL.SDL_LogPriority.SDL_LOG_PRIORITY_VERBOSE:
                    return "verbose";

                case SDL.SDL_LogPriority.SDL_LOG_PRIORITY_DEBUG:
                    return "debug";

                case SDL.SDL_LogPriority.SDL_LOG_PRIORITY_INFO:
                    return "info";

                case SDL.SDL_LogPriority.SDL_LOG_PRIORITY_WARN:
                    return "warn";

                case SDL.SDL_LogPriority.SDL_LOG_PRIORITY_ERROR:
                    return "error";

                case SDL.SDL_LogPriority.SDL_LOG_PRIORITY_CRITICAL:
                    return "critical";

                default:
                    return "unknown";
            }
        }

        /// <summary>
        /// Gets the readable string for this <see cref="SDL.SDL_DisplayMode"/>.
        /// </summary>
        /// <returns>
        /// <c>string</c> in the format of <c>1920x1080@60</c>.
        /// </returns>
        public static string ReadableString(this SDL.SDL_DisplayMode mode) => $"{mode.w}x{mode.h}@{mode.refresh_rate}";

        /// <summary>
        /// Gets the SDL error, and then clears it.
        /// </summary>
        public static string GetAndClearError()
        {
            string error = SDL.SDL_GetError();
            SDL.SDL_ClearError();
            return error;
        }

        private static bool tryGetTouchDeviceIndex(long touchId, out int index)
        {
            int n = SDL.SDL_GetNumTouchDevices();

            for (int i = 0; i < n; i++)
            {
                long currentTouchId = SDL.SDL_GetTouchDevice(i);

                if (touchId == currentTouchId)
                {
                    index = i;
                    return true;
                }
            }

            index = -1;
            return false;
        }

        /// <summary>
        /// Gets the <paramref name="name"/> of the touch device for this <see cref="SDL.SDL_TouchFingerEvent"/>.
        /// </summary>
        /// <remarks>
        /// On Windows, this will return <c>"touch"</c> for touchscreen events or <c>"pen"</c> for pen/tablet events.
        /// </remarks>
        public static bool TryGetTouchName(this SDL.SDL_TouchFingerEvent e, out string name)
        {
            if (tryGetTouchDeviceIndex(e.touchId, out int index))
            {
                name = SDL.SDL_GetTouchName(index);
                return name != null;
            }

            name = null;
            return false;
        }
    }
}
