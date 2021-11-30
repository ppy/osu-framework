// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
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
        public static Key ToKey(this SDL.SDL_Keysym sdlKeysym)
        {
            // Apple devices don't have the notion of NumLock (they have a Clear key instead).
            // treat them as if they always have NumLock on (the numpad always performs its primary actions).
            bool numLockOn = sdlKeysym.mod.HasFlagFast(SDL.SDL_Keymod.KMOD_NUM) || RuntimeInfo.IsApple;

            switch (sdlKeysym.scancode)
            {
                default:
                case SDL.SDL_Scancode.SDL_SCANCODE_UNKNOWN:
                    return Key.Unknown;

                case SDL.SDL_Scancode.SDL_SCANCODE_KP_COMMA:
                    return Key.Comma;

                case SDL.SDL_Scancode.SDL_SCANCODE_KP_TAB:
                    return Key.Tab;

                case SDL.SDL_Scancode.SDL_SCANCODE_KP_BACKSPACE:
                    return Key.BackSpace;

                case SDL.SDL_Scancode.SDL_SCANCODE_KP_A:
                    return Key.A;

                case SDL.SDL_Scancode.SDL_SCANCODE_KP_B:
                    return Key.B;

                case SDL.SDL_Scancode.SDL_SCANCODE_KP_C:
                    return Key.C;

                case SDL.SDL_Scancode.SDL_SCANCODE_KP_D:
                    return Key.D;

                case SDL.SDL_Scancode.SDL_SCANCODE_KP_E:
                    return Key.E;

                case SDL.SDL_Scancode.SDL_SCANCODE_KP_F:
                    return Key.F;

                case SDL.SDL_Scancode.SDL_SCANCODE_KP_SPACE:
                    return Key.Space;

                case SDL.SDL_Scancode.SDL_SCANCODE_KP_CLEAR:
                    return Key.Clear;

                case SDL.SDL_Scancode.SDL_SCANCODE_RETURN:
                    return Key.Enter;

                case SDL.SDL_Scancode.SDL_SCANCODE_ESCAPE:
                    return Key.Escape;

                case SDL.SDL_Scancode.SDL_SCANCODE_BACKSPACE:
                    return Key.BackSpace;

                case SDL.SDL_Scancode.SDL_SCANCODE_TAB:
                    return Key.Tab;

                case SDL.SDL_Scancode.SDL_SCANCODE_SPACE:
                    return Key.Space;

                case SDL.SDL_Scancode.SDL_SCANCODE_APOSTROPHE:
                    return Key.Quote;

                case SDL.SDL_Scancode.SDL_SCANCODE_COMMA:
                    return Key.Comma;

                case SDL.SDL_Scancode.SDL_SCANCODE_MINUS:
                    return Key.Minus;

                case SDL.SDL_Scancode.SDL_SCANCODE_PERIOD:
                    return Key.Period;

                case SDL.SDL_Scancode.SDL_SCANCODE_SLASH:
                    return Key.Slash;

                case SDL.SDL_Scancode.SDL_SCANCODE_0:
                    return Key.Number0;

                case SDL.SDL_Scancode.SDL_SCANCODE_1:
                    return Key.Number1;

                case SDL.SDL_Scancode.SDL_SCANCODE_2:
                    return Key.Number2;

                case SDL.SDL_Scancode.SDL_SCANCODE_3:
                    return Key.Number3;

                case SDL.SDL_Scancode.SDL_SCANCODE_4:
                    return Key.Number4;

                case SDL.SDL_Scancode.SDL_SCANCODE_5:
                    return Key.Number5;

                case SDL.SDL_Scancode.SDL_SCANCODE_6:
                    return Key.Number6;

                case SDL.SDL_Scancode.SDL_SCANCODE_7:
                    return Key.Number7;

                case SDL.SDL_Scancode.SDL_SCANCODE_8:
                    return Key.Number8;

                case SDL.SDL_Scancode.SDL_SCANCODE_9:
                    return Key.Number9;

                case SDL.SDL_Scancode.SDL_SCANCODE_SEMICOLON:
                    return Key.Semicolon;

                case SDL.SDL_Scancode.SDL_SCANCODE_EQUALS:
                    return Key.Plus;

                case SDL.SDL_Scancode.SDL_SCANCODE_LEFTBRACKET:
                    return Key.BracketLeft;

                case SDL.SDL_Scancode.SDL_SCANCODE_BACKSLASH:
                    return Key.BackSlash;

                case SDL.SDL_Scancode.SDL_SCANCODE_RIGHTBRACKET:
                    return Key.BracketRight;

                case SDL.SDL_Scancode.SDL_SCANCODE_GRAVE:
                    return Key.Tilde;

                case SDL.SDL_Scancode.SDL_SCANCODE_A:
                    return Key.A;

                case SDL.SDL_Scancode.SDL_SCANCODE_B:
                    return Key.B;

                case SDL.SDL_Scancode.SDL_SCANCODE_C:
                    return Key.C;

                case SDL.SDL_Scancode.SDL_SCANCODE_D:
                    return Key.D;

                case SDL.SDL_Scancode.SDL_SCANCODE_E:
                    return Key.E;

                case SDL.SDL_Scancode.SDL_SCANCODE_F:
                    return Key.F;

                case SDL.SDL_Scancode.SDL_SCANCODE_G:
                    return Key.G;

                case SDL.SDL_Scancode.SDL_SCANCODE_H:
                    return Key.H;

                case SDL.SDL_Scancode.SDL_SCANCODE_I:
                    return Key.I;

                case SDL.SDL_Scancode.SDL_SCANCODE_J:
                    return Key.J;

                case SDL.SDL_Scancode.SDL_SCANCODE_K:
                    return Key.K;

                case SDL.SDL_Scancode.SDL_SCANCODE_L:
                    return Key.L;

                case SDL.SDL_Scancode.SDL_SCANCODE_M:
                    return Key.M;

                case SDL.SDL_Scancode.SDL_SCANCODE_N:
                    return Key.N;

                case SDL.SDL_Scancode.SDL_SCANCODE_O:
                    return Key.O;

                case SDL.SDL_Scancode.SDL_SCANCODE_P:
                    return Key.P;

                case SDL.SDL_Scancode.SDL_SCANCODE_Q:
                    return Key.Q;

                case SDL.SDL_Scancode.SDL_SCANCODE_R:
                    return Key.R;

                case SDL.SDL_Scancode.SDL_SCANCODE_S:
                    return Key.S;

                case SDL.SDL_Scancode.SDL_SCANCODE_T:
                    return Key.T;

                case SDL.SDL_Scancode.SDL_SCANCODE_U:
                    return Key.U;

                case SDL.SDL_Scancode.SDL_SCANCODE_V:
                    return Key.V;

                case SDL.SDL_Scancode.SDL_SCANCODE_W:
                    return Key.W;

                case SDL.SDL_Scancode.SDL_SCANCODE_X:
                    return Key.X;

                case SDL.SDL_Scancode.SDL_SCANCODE_Y:
                    return Key.Y;

                case SDL.SDL_Scancode.SDL_SCANCODE_Z:
                    return Key.Z;

                case SDL.SDL_Scancode.SDL_SCANCODE_CAPSLOCK:
                    return Key.CapsLock;

                case SDL.SDL_Scancode.SDL_SCANCODE_F1:
                    return Key.F1;

                case SDL.SDL_Scancode.SDL_SCANCODE_F2:
                    return Key.F2;

                case SDL.SDL_Scancode.SDL_SCANCODE_F3:
                    return Key.F3;

                case SDL.SDL_Scancode.SDL_SCANCODE_F4:
                    return Key.F4;

                case SDL.SDL_Scancode.SDL_SCANCODE_F5:
                    return Key.F5;

                case SDL.SDL_Scancode.SDL_SCANCODE_F6:
                    return Key.F6;

                case SDL.SDL_Scancode.SDL_SCANCODE_F7:
                    return Key.F7;

                case SDL.SDL_Scancode.SDL_SCANCODE_F8:
                    return Key.F8;

                case SDL.SDL_Scancode.SDL_SCANCODE_F9:
                    return Key.F9;

                case SDL.SDL_Scancode.SDL_SCANCODE_F10:
                    return Key.F10;

                case SDL.SDL_Scancode.SDL_SCANCODE_F11:
                    return Key.F11;

                case SDL.SDL_Scancode.SDL_SCANCODE_F12:
                    return Key.F12;

                case SDL.SDL_Scancode.SDL_SCANCODE_PRINTSCREEN:
                    return Key.PrintScreen;

                case SDL.SDL_Scancode.SDL_SCANCODE_SCROLLLOCK:
                    return Key.ScrollLock;

                case SDL.SDL_Scancode.SDL_SCANCODE_PAUSE:
                    return Key.Pause;

                case SDL.SDL_Scancode.SDL_SCANCODE_INSERT:
                    return Key.Insert;

                case SDL.SDL_Scancode.SDL_SCANCODE_HOME:
                    return Key.Home;

                case SDL.SDL_Scancode.SDL_SCANCODE_PAGEUP:
                    return Key.PageUp;

                case SDL.SDL_Scancode.SDL_SCANCODE_DELETE:
                    return Key.Delete;

                case SDL.SDL_Scancode.SDL_SCANCODE_END:
                    return Key.End;

                case SDL.SDL_Scancode.SDL_SCANCODE_PAGEDOWN:
                    return Key.PageDown;

                case SDL.SDL_Scancode.SDL_SCANCODE_RIGHT:
                    return Key.Right;

                case SDL.SDL_Scancode.SDL_SCANCODE_LEFT:
                    return Key.Left;

                case SDL.SDL_Scancode.SDL_SCANCODE_DOWN:
                    return Key.Down;

                case SDL.SDL_Scancode.SDL_SCANCODE_UP:
                    return Key.Up;

                case SDL.SDL_Scancode.SDL_SCANCODE_NUMLOCKCLEAR:
                    return Key.NumLock;

                case SDL.SDL_Scancode.SDL_SCANCODE_KP_DIVIDE:
                    return Key.KeypadDivide;

                case SDL.SDL_Scancode.SDL_SCANCODE_KP_MULTIPLY:
                    return Key.KeypadMultiply;

                case SDL.SDL_Scancode.SDL_SCANCODE_KP_MINUS:
                    return Key.KeypadMinus;

                case SDL.SDL_Scancode.SDL_SCANCODE_KP_PLUS:
                    return Key.KeypadPlus;

                case SDL.SDL_Scancode.SDL_SCANCODE_KP_ENTER:
                    return Key.KeypadEnter;

                case SDL.SDL_Scancode.SDL_SCANCODE_KP_1:
                    return numLockOn ? Key.Keypad1 : Key.End;

                case SDL.SDL_Scancode.SDL_SCANCODE_KP_2:
                    return numLockOn ? Key.Keypad2 : Key.Down;

                case SDL.SDL_Scancode.SDL_SCANCODE_KP_3:
                    return numLockOn ? Key.Keypad3 : Key.PageDown;

                case SDL.SDL_Scancode.SDL_SCANCODE_KP_4:
                    return numLockOn ? Key.Keypad4 : Key.Left;

                case SDL.SDL_Scancode.SDL_SCANCODE_KP_5:
                    return numLockOn ? Key.Keypad5 : Key.Clear;

                case SDL.SDL_Scancode.SDL_SCANCODE_KP_6:
                    return numLockOn ? Key.Keypad6 : Key.Right;

                case SDL.SDL_Scancode.SDL_SCANCODE_KP_7:
                    return numLockOn ? Key.Keypad7 : Key.Home;

                case SDL.SDL_Scancode.SDL_SCANCODE_KP_8:
                    return numLockOn ? Key.Keypad8 : Key.Up;

                case SDL.SDL_Scancode.SDL_SCANCODE_KP_9:
                    return numLockOn ? Key.Keypad9 : Key.PageUp;

                case SDL.SDL_Scancode.SDL_SCANCODE_KP_0:
                    return numLockOn ? Key.Keypad0 : Key.Insert;

                case SDL.SDL_Scancode.SDL_SCANCODE_KP_PERIOD:
                    return numLockOn ? Key.KeypadPeriod : Key.Delete;

                case SDL.SDL_Scancode.SDL_SCANCODE_NONUSBACKSLASH:
                    return Key.NonUSBackSlash;

                case SDL.SDL_Scancode.SDL_SCANCODE_F13:
                    return Key.F13;

                case SDL.SDL_Scancode.SDL_SCANCODE_F14:
                    return Key.F14;

                case SDL.SDL_Scancode.SDL_SCANCODE_F15:
                    return Key.F15;

                case SDL.SDL_Scancode.SDL_SCANCODE_F16:
                    return Key.F16;

                case SDL.SDL_Scancode.SDL_SCANCODE_F17:
                    return Key.F17;

                case SDL.SDL_Scancode.SDL_SCANCODE_F18:
                    return Key.F18;

                case SDL.SDL_Scancode.SDL_SCANCODE_F19:
                    return Key.F19;

                case SDL.SDL_Scancode.SDL_SCANCODE_F20:
                    return Key.F20;

                case SDL.SDL_Scancode.SDL_SCANCODE_F21:
                    return Key.F21;

                case SDL.SDL_Scancode.SDL_SCANCODE_F22:
                    return Key.F22;

                case SDL.SDL_Scancode.SDL_SCANCODE_F23:
                    return Key.F23;

                case SDL.SDL_Scancode.SDL_SCANCODE_F24:
                    return Key.F24;

                case SDL.SDL_Scancode.SDL_SCANCODE_MENU:
                    return Key.Menu;

                case SDL.SDL_Scancode.SDL_SCANCODE_STOP:
                    return Key.Stop;

                case SDL.SDL_Scancode.SDL_SCANCODE_MUTE:
                    return Key.Mute;

                case SDL.SDL_Scancode.SDL_SCANCODE_VOLUMEUP:
                    return Key.VolumeUp;

                case SDL.SDL_Scancode.SDL_SCANCODE_VOLUMEDOWN:
                    return Key.VolumeDown;

                case SDL.SDL_Scancode.SDL_SCANCODE_CLEAR:
                    return Key.Clear;

                case SDL.SDL_Scancode.SDL_SCANCODE_DECIMALSEPARATOR:
                    return Key.KeypadDecimal;

                case SDL.SDL_Scancode.SDL_SCANCODE_LCTRL:
                    return Key.ControlLeft;

                case SDL.SDL_Scancode.SDL_SCANCODE_LSHIFT:
                    return Key.ShiftLeft;

                case SDL.SDL_Scancode.SDL_SCANCODE_LALT:
                    return Key.AltLeft;

                case SDL.SDL_Scancode.SDL_SCANCODE_LGUI:
                    return Key.WinLeft;

                case SDL.SDL_Scancode.SDL_SCANCODE_RCTRL:
                    return Key.ControlRight;

                case SDL.SDL_Scancode.SDL_SCANCODE_RSHIFT:
                    return Key.ShiftRight;

                case SDL.SDL_Scancode.SDL_SCANCODE_RALT:
                    return Key.AltRight;

                case SDL.SDL_Scancode.SDL_SCANCODE_RGUI:
                    return Key.WinRight;

                case SDL.SDL_Scancode.SDL_SCANCODE_AUDIONEXT:
                    return Key.TrackNext;

                case SDL.SDL_Scancode.SDL_SCANCODE_AUDIOPREV:
                    return Key.TrackPrevious;

                case SDL.SDL_Scancode.SDL_SCANCODE_AUDIOSTOP:
                    return Key.Stop;

                case SDL.SDL_Scancode.SDL_SCANCODE_AUDIOPLAY:
                    return Key.PlayPause;

                case SDL.SDL_Scancode.SDL_SCANCODE_AUDIOMUTE:
                    return Key.Mute;

                case SDL.SDL_Scancode.SDL_SCANCODE_SLEEP:
                    return Key.Sleep;
            }
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
            switch (inputKey)
            {
                default:
                case InputKey.Shift:
                case InputKey.Control:
                case InputKey.Alt:
                case InputKey.Super:
                case InputKey.F25:
                case InputKey.F26:
                case InputKey.F27:
                case InputKey.F28:
                case InputKey.F29:
                case InputKey.F30:
                case InputKey.F31:
                case InputKey.F32:
                case InputKey.F33:
                case InputKey.F34:
                case InputKey.F35:
                case InputKey.Clear:
                    return SDL.SDL_Scancode.SDL_SCANCODE_UNKNOWN;

                case InputKey.Menu:
                    return SDL.SDL_Scancode.SDL_SCANCODE_MENU;

                case InputKey.F1:
                    return SDL.SDL_Scancode.SDL_SCANCODE_F1;

                case InputKey.F2:
                    return SDL.SDL_Scancode.SDL_SCANCODE_F2;

                case InputKey.F3:
                    return SDL.SDL_Scancode.SDL_SCANCODE_F3;

                case InputKey.F4:
                    return SDL.SDL_Scancode.SDL_SCANCODE_F4;

                case InputKey.F5:
                    return SDL.SDL_Scancode.SDL_SCANCODE_F5;

                case InputKey.F6:
                    return SDL.SDL_Scancode.SDL_SCANCODE_F6;

                case InputKey.F7:
                    return SDL.SDL_Scancode.SDL_SCANCODE_F7;

                case InputKey.F8:
                    return SDL.SDL_Scancode.SDL_SCANCODE_F8;

                case InputKey.F9:
                    return SDL.SDL_Scancode.SDL_SCANCODE_F9;

                case InputKey.F10:
                    return SDL.SDL_Scancode.SDL_SCANCODE_F10;

                case InputKey.F11:
                    return SDL.SDL_Scancode.SDL_SCANCODE_F11;

                case InputKey.F12:
                    return SDL.SDL_Scancode.SDL_SCANCODE_F12;

                case InputKey.F13:
                    return SDL.SDL_Scancode.SDL_SCANCODE_F13;

                case InputKey.F14:
                    return SDL.SDL_Scancode.SDL_SCANCODE_F14;

                case InputKey.F15:
                    return SDL.SDL_Scancode.SDL_SCANCODE_F15;

                case InputKey.F16:
                    return SDL.SDL_Scancode.SDL_SCANCODE_F16;

                case InputKey.F17:
                    return SDL.SDL_Scancode.SDL_SCANCODE_F17;

                case InputKey.F18:
                    return SDL.SDL_Scancode.SDL_SCANCODE_F18;

                case InputKey.F19:
                    return SDL.SDL_Scancode.SDL_SCANCODE_F19;

                case InputKey.F20:
                    return SDL.SDL_Scancode.SDL_SCANCODE_F20;

                case InputKey.F21:
                    return SDL.SDL_Scancode.SDL_SCANCODE_F21;

                case InputKey.F22:
                    return SDL.SDL_Scancode.SDL_SCANCODE_F22;

                case InputKey.F23:
                    return SDL.SDL_Scancode.SDL_SCANCODE_F23;

                case InputKey.F24:
                    return SDL.SDL_Scancode.SDL_SCANCODE_F24;

                case InputKey.Up:
                    return SDL.SDL_Scancode.SDL_SCANCODE_UP;

                case InputKey.Down:
                    return SDL.SDL_Scancode.SDL_SCANCODE_DOWN;

                case InputKey.Left:
                    return SDL.SDL_Scancode.SDL_SCANCODE_LEFT;

                case InputKey.Right:
                    return SDL.SDL_Scancode.SDL_SCANCODE_RIGHT;

                case InputKey.Enter:
                    return SDL.SDL_Scancode.SDL_SCANCODE_RETURN;

                case InputKey.Escape:
                    return SDL.SDL_Scancode.SDL_SCANCODE_ESCAPE;

                case InputKey.Space:
                    return SDL.SDL_Scancode.SDL_SCANCODE_SPACE;

                case InputKey.Tab:
                    return SDL.SDL_Scancode.SDL_SCANCODE_TAB;

                case InputKey.BackSpace:
                    return SDL.SDL_Scancode.SDL_SCANCODE_BACKSPACE;

                case InputKey.Insert:
                    return SDL.SDL_Scancode.SDL_SCANCODE_INSERT;

                case InputKey.Delete:
                    return SDL.SDL_Scancode.SDL_SCANCODE_DELETE;

                case InputKey.PageUp:
                    return SDL.SDL_Scancode.SDL_SCANCODE_PAGEUP;

                case InputKey.PageDown:
                    return SDL.SDL_Scancode.SDL_SCANCODE_PAGEDOWN;

                case InputKey.Home:
                    return SDL.SDL_Scancode.SDL_SCANCODE_HOME;

                case InputKey.End:
                    return SDL.SDL_Scancode.SDL_SCANCODE_END;

                case InputKey.CapsLock:
                    return SDL.SDL_Scancode.SDL_SCANCODE_CAPSLOCK;

                case InputKey.ScrollLock:
                    return SDL.SDL_Scancode.SDL_SCANCODE_SCROLLLOCK;

                case InputKey.PrintScreen:
                    return SDL.SDL_Scancode.SDL_SCANCODE_PRINTSCREEN;

                case InputKey.Pause:
                    return SDL.SDL_Scancode.SDL_SCANCODE_PAUSE;

                case InputKey.NumLock:
                    return SDL.SDL_Scancode.SDL_SCANCODE_NUMLOCKCLEAR;

                case InputKey.Sleep:
                    return SDL.SDL_Scancode.SDL_SCANCODE_SLEEP;

                case InputKey.Keypad0:
                    return SDL.SDL_Scancode.SDL_SCANCODE_KP_0;

                case InputKey.Keypad1:
                    return SDL.SDL_Scancode.SDL_SCANCODE_KP_1;

                case InputKey.Keypad2:
                    return SDL.SDL_Scancode.SDL_SCANCODE_KP_2;

                case InputKey.Keypad3:
                    return SDL.SDL_Scancode.SDL_SCANCODE_KP_3;

                case InputKey.Keypad4:
                    return SDL.SDL_Scancode.SDL_SCANCODE_KP_4;

                case InputKey.Keypad5:
                    return SDL.SDL_Scancode.SDL_SCANCODE_KP_5;

                case InputKey.Keypad6:
                    return SDL.SDL_Scancode.SDL_SCANCODE_KP_6;

                case InputKey.Keypad7:
                    return SDL.SDL_Scancode.SDL_SCANCODE_KP_7;

                case InputKey.Keypad8:
                    return SDL.SDL_Scancode.SDL_SCANCODE_KP_8;

                case InputKey.Keypad9:
                    return SDL.SDL_Scancode.SDL_SCANCODE_KP_9;

                case InputKey.KeypadDivide:
                    return SDL.SDL_Scancode.SDL_SCANCODE_KP_DIVIDE;

                case InputKey.KeypadMultiply:
                    return SDL.SDL_Scancode.SDL_SCANCODE_KP_MULTIPLY;

                case InputKey.KeypadMinus:
                    return SDL.SDL_Scancode.SDL_SCANCODE_KP_MINUS;

                case InputKey.KeypadPlus:
                    return SDL.SDL_Scancode.SDL_SCANCODE_KP_PLUS;

                case InputKey.KeypadPeriod:
                    return SDL.SDL_Scancode.SDL_SCANCODE_KP_PERIOD;

                case InputKey.KeypadEnter:
                    return SDL.SDL_Scancode.SDL_SCANCODE_KP_ENTER;

                case InputKey.A:
                    return SDL.SDL_Scancode.SDL_SCANCODE_A;

                case InputKey.B:
                    return SDL.SDL_Scancode.SDL_SCANCODE_B;

                case InputKey.C:
                    return SDL.SDL_Scancode.SDL_SCANCODE_C;

                case InputKey.D:
                    return SDL.SDL_Scancode.SDL_SCANCODE_D;

                case InputKey.E:
                    return SDL.SDL_Scancode.SDL_SCANCODE_E;

                case InputKey.F:
                    return SDL.SDL_Scancode.SDL_SCANCODE_F;

                case InputKey.G:
                    return SDL.SDL_Scancode.SDL_SCANCODE_G;

                case InputKey.H:
                    return SDL.SDL_Scancode.SDL_SCANCODE_H;

                case InputKey.I:
                    return SDL.SDL_Scancode.SDL_SCANCODE_I;

                case InputKey.J:
                    return SDL.SDL_Scancode.SDL_SCANCODE_J;

                case InputKey.K:
                    return SDL.SDL_Scancode.SDL_SCANCODE_K;

                case InputKey.L:
                    return SDL.SDL_Scancode.SDL_SCANCODE_L;

                case InputKey.M:
                    return SDL.SDL_Scancode.SDL_SCANCODE_M;

                case InputKey.N:
                    return SDL.SDL_Scancode.SDL_SCANCODE_N;

                case InputKey.O:
                    return SDL.SDL_Scancode.SDL_SCANCODE_O;

                case InputKey.P:
                    return SDL.SDL_Scancode.SDL_SCANCODE_P;

                case InputKey.Q:
                    return SDL.SDL_Scancode.SDL_SCANCODE_Q;

                case InputKey.R:
                    return SDL.SDL_Scancode.SDL_SCANCODE_R;

                case InputKey.S:
                    return SDL.SDL_Scancode.SDL_SCANCODE_S;

                case InputKey.T:
                    return SDL.SDL_Scancode.SDL_SCANCODE_T;

                case InputKey.U:
                    return SDL.SDL_Scancode.SDL_SCANCODE_U;

                case InputKey.V:
                    return SDL.SDL_Scancode.SDL_SCANCODE_V;

                case InputKey.W:
                    return SDL.SDL_Scancode.SDL_SCANCODE_W;

                case InputKey.X:
                    return SDL.SDL_Scancode.SDL_SCANCODE_X;

                case InputKey.Y:
                    return SDL.SDL_Scancode.SDL_SCANCODE_Y;

                case InputKey.Z:
                    return SDL.SDL_Scancode.SDL_SCANCODE_Z;

                case InputKey.Number0:
                    return SDL.SDL_Scancode.SDL_SCANCODE_0;

                case InputKey.Number1:
                    return SDL.SDL_Scancode.SDL_SCANCODE_1;

                case InputKey.Number2:
                    return SDL.SDL_Scancode.SDL_SCANCODE_2;

                case InputKey.Number3:
                    return SDL.SDL_Scancode.SDL_SCANCODE_3;

                case InputKey.Number4:
                    return SDL.SDL_Scancode.SDL_SCANCODE_4;

                case InputKey.Number5:
                    return SDL.SDL_Scancode.SDL_SCANCODE_5;

                case InputKey.Number6:
                    return SDL.SDL_Scancode.SDL_SCANCODE_6;

                case InputKey.Number7:
                    return SDL.SDL_Scancode.SDL_SCANCODE_7;

                case InputKey.Number8:
                    return SDL.SDL_Scancode.SDL_SCANCODE_8;

                case InputKey.Number9:
                    return SDL.SDL_Scancode.SDL_SCANCODE_9;

                case InputKey.Grave:
                    return SDL.SDL_Scancode.SDL_SCANCODE_GRAVE;

                case InputKey.Minus:
                    return SDL.SDL_Scancode.SDL_SCANCODE_MINUS;

                case InputKey.Plus:
                    return SDL.SDL_Scancode.SDL_SCANCODE_EQUALS;

                case InputKey.BracketLeft:
                    return SDL.SDL_Scancode.SDL_SCANCODE_LEFTBRACKET;

                case InputKey.BracketRight:
                    return SDL.SDL_Scancode.SDL_SCANCODE_RIGHTBRACKET;

                case InputKey.Semicolon:
                    return SDL.SDL_Scancode.SDL_SCANCODE_SEMICOLON;

                case InputKey.Quote:
                    return SDL.SDL_Scancode.SDL_SCANCODE_APOSTROPHE;

                case InputKey.Comma:
                    return SDL.SDL_Scancode.SDL_SCANCODE_COMMA;

                case InputKey.Period:
                    return SDL.SDL_Scancode.SDL_SCANCODE_PERIOD;

                case InputKey.Slash:
                    return SDL.SDL_Scancode.SDL_SCANCODE_SLASH;

                case InputKey.BackSlash:
                    return SDL.SDL_Scancode.SDL_SCANCODE_BACKSLASH;

                case InputKey.NonUSBackSlash:
                    return SDL.SDL_Scancode.SDL_SCANCODE_NONUSBACKSLASH;

                case InputKey.Mute:
                    return SDL.SDL_Scancode.SDL_SCANCODE_AUDIOMUTE;

                case InputKey.PlayPause:
                    return SDL.SDL_Scancode.SDL_SCANCODE_AUDIOPLAY;

                case InputKey.Stop:
                    return SDL.SDL_Scancode.SDL_SCANCODE_AUDIOSTOP;

                case InputKey.VolumeUp:
                    return SDL.SDL_Scancode.SDL_SCANCODE_VOLUMEUP;

                case InputKey.VolumeDown:
                    return SDL.SDL_Scancode.SDL_SCANCODE_VOLUMEDOWN;

                case InputKey.TrackPrevious:
                    return SDL.SDL_Scancode.SDL_SCANCODE_AUDIOPREV;

                case InputKey.TrackNext:
                    return SDL.SDL_Scancode.SDL_SCANCODE_AUDIONEXT;

                case InputKey.LShift:
                    return SDL.SDL_Scancode.SDL_SCANCODE_LSHIFT;

                case InputKey.RShift:
                    return SDL.SDL_Scancode.SDL_SCANCODE_RSHIFT;

                case InputKey.LControl:
                    return SDL.SDL_Scancode.SDL_SCANCODE_LCTRL;

                case InputKey.RControl:
                    return SDL.SDL_Scancode.SDL_SCANCODE_RCTRL;

                case InputKey.LAlt:
                    return SDL.SDL_Scancode.SDL_SCANCODE_LALT;

                case InputKey.RAlt:
                    return SDL.SDL_Scancode.SDL_SCANCODE_RALT;

                case InputKey.LSuper:
                    return SDL.SDL_Scancode.SDL_SCANCODE_LGUI;

                case InputKey.RSuper:
                    return SDL.SDL_Scancode.SDL_SCANCODE_RGUI;
            }
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
            var ptr = new IntPtr(bytePointer);

            if (ptr == IntPtr.Zero)
            {
                str = null;
                return false;
            }

            str = Marshal.PtrToStringUTF8(ptr) ?? string.Empty;
            return true;
        }
    }
}
