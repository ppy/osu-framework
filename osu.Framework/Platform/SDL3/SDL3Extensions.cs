// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Drawing;
using osu.Framework.Extensions.EnumExtensions;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Input;
using osu.Framework.Input.Bindings;
using osuTK.Input;
using SDL;
using static SDL.SDL3;

namespace osu.Framework.Platform.SDL3
{
    public static class SDL3Extensions
    {
        public static Key ToKey(this SDL_KeyboardEvent sdlKeyboardEvent)
        {
            // Apple devices don't have the notion of NumLock (they have a Clear key instead).
            // treat them as if they always have NumLock on (the numpad always performs its primary actions).
            bool numLockOn = sdlKeyboardEvent.mod.HasFlagFast(SDL_Keymod.SDL_KMOD_NUM) || RuntimeInfo.IsApple;

            switch (sdlKeyboardEvent.scancode)
            {
                default:
                case SDL_Scancode.SDL_SCANCODE_UNKNOWN:
                    return Key.Unknown;

                case SDL_Scancode.SDL_SCANCODE_KP_COMMA:
                    return Key.Comma;

                case SDL_Scancode.SDL_SCANCODE_KP_TAB:
                    return Key.Tab;

                case SDL_Scancode.SDL_SCANCODE_KP_BACKSPACE:
                    return Key.BackSpace;

                case SDL_Scancode.SDL_SCANCODE_KP_A:
                    return Key.A;

                case SDL_Scancode.SDL_SCANCODE_KP_B:
                    return Key.B;

                case SDL_Scancode.SDL_SCANCODE_KP_C:
                    return Key.C;

                case SDL_Scancode.SDL_SCANCODE_KP_D:
                    return Key.D;

                case SDL_Scancode.SDL_SCANCODE_KP_E:
                    return Key.E;

                case SDL_Scancode.SDL_SCANCODE_KP_F:
                    return Key.F;

                case SDL_Scancode.SDL_SCANCODE_KP_SPACE:
                    return Key.Space;

                case SDL_Scancode.SDL_SCANCODE_KP_CLEAR:
                    return Key.Clear;

                case SDL_Scancode.SDL_SCANCODE_RETURN:
                    return Key.Enter;

                case SDL_Scancode.SDL_SCANCODE_ESCAPE:
                    return Key.Escape;

                case SDL_Scancode.SDL_SCANCODE_BACKSPACE:
                    return Key.BackSpace;

                case SDL_Scancode.SDL_SCANCODE_TAB:
                    return Key.Tab;

                case SDL_Scancode.SDL_SCANCODE_SPACE:
                    return Key.Space;

                case SDL_Scancode.SDL_SCANCODE_APOSTROPHE:
                    return Key.Quote;

                case SDL_Scancode.SDL_SCANCODE_COMMA:
                    return Key.Comma;

                case SDL_Scancode.SDL_SCANCODE_MINUS:
                    return Key.Minus;

                case SDL_Scancode.SDL_SCANCODE_PERIOD:
                    return Key.Period;

                case SDL_Scancode.SDL_SCANCODE_SLASH:
                    return Key.Slash;

                case SDL_Scancode.SDL_SCANCODE_0:
                    return Key.Number0;

                case SDL_Scancode.SDL_SCANCODE_1:
                    return Key.Number1;

                case SDL_Scancode.SDL_SCANCODE_2:
                    return Key.Number2;

                case SDL_Scancode.SDL_SCANCODE_3:
                    return Key.Number3;

                case SDL_Scancode.SDL_SCANCODE_4:
                    return Key.Number4;

                case SDL_Scancode.SDL_SCANCODE_5:
                    return Key.Number5;

                case SDL_Scancode.SDL_SCANCODE_6:
                    return Key.Number6;

                case SDL_Scancode.SDL_SCANCODE_7:
                    return Key.Number7;

                case SDL_Scancode.SDL_SCANCODE_8:
                    return Key.Number8;

                case SDL_Scancode.SDL_SCANCODE_9:
                    return Key.Number9;

                case SDL_Scancode.SDL_SCANCODE_SEMICOLON:
                    return Key.Semicolon;

                case SDL_Scancode.SDL_SCANCODE_EQUALS:
                    return Key.Plus;

                case SDL_Scancode.SDL_SCANCODE_LEFTBRACKET:
                    return Key.BracketLeft;

                case SDL_Scancode.SDL_SCANCODE_BACKSLASH:
                    return Key.BackSlash;

                case SDL_Scancode.SDL_SCANCODE_RIGHTBRACKET:
                    return Key.BracketRight;

                case SDL_Scancode.SDL_SCANCODE_GRAVE:
                    return Key.Tilde;

                case SDL_Scancode.SDL_SCANCODE_A:
                    return Key.A;

                case SDL_Scancode.SDL_SCANCODE_B:
                    return Key.B;

                case SDL_Scancode.SDL_SCANCODE_C:
                    return Key.C;

                case SDL_Scancode.SDL_SCANCODE_D:
                    return Key.D;

                case SDL_Scancode.SDL_SCANCODE_E:
                    return Key.E;

                case SDL_Scancode.SDL_SCANCODE_F:
                    return Key.F;

                case SDL_Scancode.SDL_SCANCODE_G:
                    return Key.G;

                case SDL_Scancode.SDL_SCANCODE_H:
                    return Key.H;

                case SDL_Scancode.SDL_SCANCODE_I:
                    return Key.I;

                case SDL_Scancode.SDL_SCANCODE_J:
                    return Key.J;

                case SDL_Scancode.SDL_SCANCODE_K:
                    return Key.K;

                case SDL_Scancode.SDL_SCANCODE_L:
                    return Key.L;

                case SDL_Scancode.SDL_SCANCODE_M:
                    return Key.M;

                case SDL_Scancode.SDL_SCANCODE_N:
                    return Key.N;

                case SDL_Scancode.SDL_SCANCODE_O:
                    return Key.O;

                case SDL_Scancode.SDL_SCANCODE_P:
                    return Key.P;

                case SDL_Scancode.SDL_SCANCODE_Q:
                    return Key.Q;

                case SDL_Scancode.SDL_SCANCODE_R:
                    return Key.R;

                case SDL_Scancode.SDL_SCANCODE_S:
                    return Key.S;

                case SDL_Scancode.SDL_SCANCODE_T:
                    return Key.T;

                case SDL_Scancode.SDL_SCANCODE_U:
                    return Key.U;

                case SDL_Scancode.SDL_SCANCODE_V:
                    return Key.V;

                case SDL_Scancode.SDL_SCANCODE_W:
                    return Key.W;

                case SDL_Scancode.SDL_SCANCODE_X:
                    return Key.X;

                case SDL_Scancode.SDL_SCANCODE_Y:
                    return Key.Y;

                case SDL_Scancode.SDL_SCANCODE_Z:
                    return Key.Z;

                case SDL_Scancode.SDL_SCANCODE_CAPSLOCK:
                    return Key.CapsLock;

                case SDL_Scancode.SDL_SCANCODE_F1:
                    return Key.F1;

                case SDL_Scancode.SDL_SCANCODE_F2:
                    return Key.F2;

                case SDL_Scancode.SDL_SCANCODE_F3:
                    return Key.F3;

                case SDL_Scancode.SDL_SCANCODE_F4:
                    return Key.F4;

                case SDL_Scancode.SDL_SCANCODE_F5:
                    return Key.F5;

                case SDL_Scancode.SDL_SCANCODE_F6:
                    return Key.F6;

                case SDL_Scancode.SDL_SCANCODE_F7:
                    return Key.F7;

                case SDL_Scancode.SDL_SCANCODE_F8:
                    return Key.F8;

                case SDL_Scancode.SDL_SCANCODE_F9:
                    return Key.F9;

                case SDL_Scancode.SDL_SCANCODE_F10:
                    return Key.F10;

                case SDL_Scancode.SDL_SCANCODE_F11:
                    return Key.F11;

                case SDL_Scancode.SDL_SCANCODE_F12:
                    return Key.F12;

                case SDL_Scancode.SDL_SCANCODE_PRINTSCREEN:
                    return Key.PrintScreen;

                case SDL_Scancode.SDL_SCANCODE_SCROLLLOCK:
                    return Key.ScrollLock;

                case SDL_Scancode.SDL_SCANCODE_PAUSE:
                    return Key.Pause;

                case SDL_Scancode.SDL_SCANCODE_INSERT:
                    return Key.Insert;

                case SDL_Scancode.SDL_SCANCODE_HOME:
                    return Key.Home;

                case SDL_Scancode.SDL_SCANCODE_PAGEUP:
                    return Key.PageUp;

                case SDL_Scancode.SDL_SCANCODE_DELETE:
                    return Key.Delete;

                case SDL_Scancode.SDL_SCANCODE_END:
                    return Key.End;

                case SDL_Scancode.SDL_SCANCODE_PAGEDOWN:
                    return Key.PageDown;

                case SDL_Scancode.SDL_SCANCODE_RIGHT:
                    return Key.Right;

                case SDL_Scancode.SDL_SCANCODE_LEFT:
                    return Key.Left;

                case SDL_Scancode.SDL_SCANCODE_DOWN:
                    return Key.Down;

                case SDL_Scancode.SDL_SCANCODE_UP:
                    return Key.Up;

                case SDL_Scancode.SDL_SCANCODE_NUMLOCKCLEAR:
                    return Key.NumLock;

                case SDL_Scancode.SDL_SCANCODE_KP_DIVIDE:
                    return Key.KeypadDivide;

                case SDL_Scancode.SDL_SCANCODE_KP_MULTIPLY:
                    return Key.KeypadMultiply;

                case SDL_Scancode.SDL_SCANCODE_KP_MINUS:
                    return Key.KeypadMinus;

                case SDL_Scancode.SDL_SCANCODE_KP_PLUS:
                    return Key.KeypadPlus;

                case SDL_Scancode.SDL_SCANCODE_KP_ENTER:
                    return Key.KeypadEnter;

                case SDL_Scancode.SDL_SCANCODE_KP_1:
                    return numLockOn ? Key.Keypad1 : Key.End;

                case SDL_Scancode.SDL_SCANCODE_KP_2:
                    return numLockOn ? Key.Keypad2 : Key.Down;

                case SDL_Scancode.SDL_SCANCODE_KP_3:
                    return numLockOn ? Key.Keypad3 : Key.PageDown;

                case SDL_Scancode.SDL_SCANCODE_KP_4:
                    return numLockOn ? Key.Keypad4 : Key.Left;

                case SDL_Scancode.SDL_SCANCODE_KP_5:
                    return numLockOn ? Key.Keypad5 : Key.Clear;

                case SDL_Scancode.SDL_SCANCODE_KP_6:
                    return numLockOn ? Key.Keypad6 : Key.Right;

                case SDL_Scancode.SDL_SCANCODE_KP_7:
                    return numLockOn ? Key.Keypad7 : Key.Home;

                case SDL_Scancode.SDL_SCANCODE_KP_8:
                    return numLockOn ? Key.Keypad8 : Key.Up;

                case SDL_Scancode.SDL_SCANCODE_KP_9:
                    return numLockOn ? Key.Keypad9 : Key.PageUp;

                case SDL_Scancode.SDL_SCANCODE_KP_0:
                    return numLockOn ? Key.Keypad0 : Key.Insert;

                case SDL_Scancode.SDL_SCANCODE_KP_PERIOD:
                    return numLockOn ? Key.KeypadPeriod : Key.Delete;

                case SDL_Scancode.SDL_SCANCODE_NONUSBACKSLASH:
                    return Key.NonUSBackSlash;

                case SDL_Scancode.SDL_SCANCODE_F13:
                    return Key.F13;

                case SDL_Scancode.SDL_SCANCODE_F14:
                    return Key.F14;

                case SDL_Scancode.SDL_SCANCODE_F15:
                    return Key.F15;

                case SDL_Scancode.SDL_SCANCODE_F16:
                    return Key.F16;

                case SDL_Scancode.SDL_SCANCODE_F17:
                    return Key.F17;

                case SDL_Scancode.SDL_SCANCODE_F18:
                    return Key.F18;

                case SDL_Scancode.SDL_SCANCODE_F19:
                    return Key.F19;

                case SDL_Scancode.SDL_SCANCODE_F20:
                    return Key.F20;

                case SDL_Scancode.SDL_SCANCODE_F21:
                    return Key.F21;

                case SDL_Scancode.SDL_SCANCODE_F22:
                    return Key.F22;

                case SDL_Scancode.SDL_SCANCODE_F23:
                    return Key.F23;

                case SDL_Scancode.SDL_SCANCODE_F24:
                    return Key.F24;

                case SDL_Scancode.SDL_SCANCODE_MENU:
                case SDL_Scancode.SDL_SCANCODE_APPLICATION:
                    return Key.Menu;

                case SDL_Scancode.SDL_SCANCODE_STOP:
                    return Key.Stop;

                case SDL_Scancode.SDL_SCANCODE_MUTE:
                    return Key.Mute;

                case SDL_Scancode.SDL_SCANCODE_VOLUMEUP:
                    return Key.VolumeUp;

                case SDL_Scancode.SDL_SCANCODE_VOLUMEDOWN:
                    return Key.VolumeDown;

                case SDL_Scancode.SDL_SCANCODE_CLEAR:
                    return Key.Clear;

                case SDL_Scancode.SDL_SCANCODE_DECIMALSEPARATOR:
                    return Key.KeypadDecimal;

                case SDL_Scancode.SDL_SCANCODE_LCTRL:
                    return Key.ControlLeft;

                case SDL_Scancode.SDL_SCANCODE_LSHIFT:
                    return Key.ShiftLeft;

                case SDL_Scancode.SDL_SCANCODE_LALT:
                    return Key.AltLeft;

                case SDL_Scancode.SDL_SCANCODE_LGUI:
                    return Key.WinLeft;

                case SDL_Scancode.SDL_SCANCODE_RCTRL:
                    return Key.ControlRight;

                case SDL_Scancode.SDL_SCANCODE_RSHIFT:
                    return Key.ShiftRight;

                case SDL_Scancode.SDL_SCANCODE_RALT:
                    return Key.AltRight;

                case SDL_Scancode.SDL_SCANCODE_RGUI:
                    return Key.WinRight;

                case SDL_Scancode.SDL_SCANCODE_MEDIA_NEXT_TRACK:
                    return Key.TrackNext;

                case SDL_Scancode.SDL_SCANCODE_MEDIA_PREVIOUS_TRACK:
                    return Key.TrackPrevious;

                case SDL_Scancode.SDL_SCANCODE_MEDIA_STOP:
                    return Key.Stop;

                case SDL_Scancode.SDL_SCANCODE_MEDIA_PLAY_PAUSE:
                    return Key.PlayPause;

                case SDL_Scancode.SDL_SCANCODE_SLEEP:
                    return Key.Sleep;

                case SDL_Scancode.SDL_SCANCODE_AC_BACK:
                    return Key.Escape;
            }
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
                    return SDL_Scancode.SDL_SCANCODE_UNKNOWN;

                case InputKey.Menu:
                    return SDL_Scancode.SDL_SCANCODE_MENU;

                case InputKey.F1:
                    return SDL_Scancode.SDL_SCANCODE_F1;

                case InputKey.F2:
                    return SDL_Scancode.SDL_SCANCODE_F2;

                case InputKey.F3:
                    return SDL_Scancode.SDL_SCANCODE_F3;

                case InputKey.F4:
                    return SDL_Scancode.SDL_SCANCODE_F4;

                case InputKey.F5:
                    return SDL_Scancode.SDL_SCANCODE_F5;

                case InputKey.F6:
                    return SDL_Scancode.SDL_SCANCODE_F6;

                case InputKey.F7:
                    return SDL_Scancode.SDL_SCANCODE_F7;

                case InputKey.F8:
                    return SDL_Scancode.SDL_SCANCODE_F8;

                case InputKey.F9:
                    return SDL_Scancode.SDL_SCANCODE_F9;

                case InputKey.F10:
                    return SDL_Scancode.SDL_SCANCODE_F10;

                case InputKey.F11:
                    return SDL_Scancode.SDL_SCANCODE_F11;

                case InputKey.F12:
                    return SDL_Scancode.SDL_SCANCODE_F12;

                case InputKey.F13:
                    return SDL_Scancode.SDL_SCANCODE_F13;

                case InputKey.F14:
                    return SDL_Scancode.SDL_SCANCODE_F14;

                case InputKey.F15:
                    return SDL_Scancode.SDL_SCANCODE_F15;

                case InputKey.F16:
                    return SDL_Scancode.SDL_SCANCODE_F16;

                case InputKey.F17:
                    return SDL_Scancode.SDL_SCANCODE_F17;

                case InputKey.F18:
                    return SDL_Scancode.SDL_SCANCODE_F18;

                case InputKey.F19:
                    return SDL_Scancode.SDL_SCANCODE_F19;

                case InputKey.F20:
                    return SDL_Scancode.SDL_SCANCODE_F20;

                case InputKey.F21:
                    return SDL_Scancode.SDL_SCANCODE_F21;

                case InputKey.F22:
                    return SDL_Scancode.SDL_SCANCODE_F22;

                case InputKey.F23:
                    return SDL_Scancode.SDL_SCANCODE_F23;

                case InputKey.F24:
                    return SDL_Scancode.SDL_SCANCODE_F24;

                case InputKey.Up:
                    return SDL_Scancode.SDL_SCANCODE_UP;

                case InputKey.Down:
                    return SDL_Scancode.SDL_SCANCODE_DOWN;

                case InputKey.Left:
                    return SDL_Scancode.SDL_SCANCODE_LEFT;

                case InputKey.Right:
                    return SDL_Scancode.SDL_SCANCODE_RIGHT;

                case InputKey.Enter:
                    return SDL_Scancode.SDL_SCANCODE_RETURN;

                case InputKey.Escape:
                    return SDL_Scancode.SDL_SCANCODE_ESCAPE;

                case InputKey.Space:
                    return SDL_Scancode.SDL_SCANCODE_SPACE;

                case InputKey.Tab:
                    return SDL_Scancode.SDL_SCANCODE_TAB;

                case InputKey.BackSpace:
                    return SDL_Scancode.SDL_SCANCODE_BACKSPACE;

                case InputKey.Insert:
                    return SDL_Scancode.SDL_SCANCODE_INSERT;

                case InputKey.Delete:
                    return SDL_Scancode.SDL_SCANCODE_DELETE;

                case InputKey.PageUp:
                    return SDL_Scancode.SDL_SCANCODE_PAGEUP;

                case InputKey.PageDown:
                    return SDL_Scancode.SDL_SCANCODE_PAGEDOWN;

                case InputKey.Home:
                    return SDL_Scancode.SDL_SCANCODE_HOME;

                case InputKey.End:
                    return SDL_Scancode.SDL_SCANCODE_END;

                case InputKey.CapsLock:
                    return SDL_Scancode.SDL_SCANCODE_CAPSLOCK;

                case InputKey.ScrollLock:
                    return SDL_Scancode.SDL_SCANCODE_SCROLLLOCK;

                case InputKey.PrintScreen:
                    return SDL_Scancode.SDL_SCANCODE_PRINTSCREEN;

                case InputKey.Pause:
                    return SDL_Scancode.SDL_SCANCODE_PAUSE;

                case InputKey.NumLock:
                    return SDL_Scancode.SDL_SCANCODE_NUMLOCKCLEAR;

                case InputKey.Sleep:
                    return SDL_Scancode.SDL_SCANCODE_SLEEP;

                case InputKey.Keypad0:
                    return SDL_Scancode.SDL_SCANCODE_KP_0;

                case InputKey.Keypad1:
                    return SDL_Scancode.SDL_SCANCODE_KP_1;

                case InputKey.Keypad2:
                    return SDL_Scancode.SDL_SCANCODE_KP_2;

                case InputKey.Keypad3:
                    return SDL_Scancode.SDL_SCANCODE_KP_3;

                case InputKey.Keypad4:
                    return SDL_Scancode.SDL_SCANCODE_KP_4;

                case InputKey.Keypad5:
                    return SDL_Scancode.SDL_SCANCODE_KP_5;

                case InputKey.Keypad6:
                    return SDL_Scancode.SDL_SCANCODE_KP_6;

                case InputKey.Keypad7:
                    return SDL_Scancode.SDL_SCANCODE_KP_7;

                case InputKey.Keypad8:
                    return SDL_Scancode.SDL_SCANCODE_KP_8;

                case InputKey.Keypad9:
                    return SDL_Scancode.SDL_SCANCODE_KP_9;

                case InputKey.KeypadDivide:
                    return SDL_Scancode.SDL_SCANCODE_KP_DIVIDE;

                case InputKey.KeypadMultiply:
                    return SDL_Scancode.SDL_SCANCODE_KP_MULTIPLY;

                case InputKey.KeypadMinus:
                    return SDL_Scancode.SDL_SCANCODE_KP_MINUS;

                case InputKey.KeypadPlus:
                    return SDL_Scancode.SDL_SCANCODE_KP_PLUS;

                case InputKey.KeypadPeriod:
                    return SDL_Scancode.SDL_SCANCODE_KP_PERIOD;

                case InputKey.KeypadEnter:
                    return SDL_Scancode.SDL_SCANCODE_KP_ENTER;

                case InputKey.A:
                    return SDL_Scancode.SDL_SCANCODE_A;

                case InputKey.B:
                    return SDL_Scancode.SDL_SCANCODE_B;

                case InputKey.C:
                    return SDL_Scancode.SDL_SCANCODE_C;

                case InputKey.D:
                    return SDL_Scancode.SDL_SCANCODE_D;

                case InputKey.E:
                    return SDL_Scancode.SDL_SCANCODE_E;

                case InputKey.F:
                    return SDL_Scancode.SDL_SCANCODE_F;

                case InputKey.G:
                    return SDL_Scancode.SDL_SCANCODE_G;

                case InputKey.H:
                    return SDL_Scancode.SDL_SCANCODE_H;

                case InputKey.I:
                    return SDL_Scancode.SDL_SCANCODE_I;

                case InputKey.J:
                    return SDL_Scancode.SDL_SCANCODE_J;

                case InputKey.K:
                    return SDL_Scancode.SDL_SCANCODE_K;

                case InputKey.L:
                    return SDL_Scancode.SDL_SCANCODE_L;

                case InputKey.M:
                    return SDL_Scancode.SDL_SCANCODE_M;

                case InputKey.N:
                    return SDL_Scancode.SDL_SCANCODE_N;

                case InputKey.O:
                    return SDL_Scancode.SDL_SCANCODE_O;

                case InputKey.P:
                    return SDL_Scancode.SDL_SCANCODE_P;

                case InputKey.Q:
                    return SDL_Scancode.SDL_SCANCODE_Q;

                case InputKey.R:
                    return SDL_Scancode.SDL_SCANCODE_R;

                case InputKey.S:
                    return SDL_Scancode.SDL_SCANCODE_S;

                case InputKey.T:
                    return SDL_Scancode.SDL_SCANCODE_T;

                case InputKey.U:
                    return SDL_Scancode.SDL_SCANCODE_U;

                case InputKey.V:
                    return SDL_Scancode.SDL_SCANCODE_V;

                case InputKey.W:
                    return SDL_Scancode.SDL_SCANCODE_W;

                case InputKey.X:
                    return SDL_Scancode.SDL_SCANCODE_X;

                case InputKey.Y:
                    return SDL_Scancode.SDL_SCANCODE_Y;

                case InputKey.Z:
                    return SDL_Scancode.SDL_SCANCODE_Z;

                case InputKey.Number0:
                    return SDL_Scancode.SDL_SCANCODE_0;

                case InputKey.Number1:
                    return SDL_Scancode.SDL_SCANCODE_1;

                case InputKey.Number2:
                    return SDL_Scancode.SDL_SCANCODE_2;

                case InputKey.Number3:
                    return SDL_Scancode.SDL_SCANCODE_3;

                case InputKey.Number4:
                    return SDL_Scancode.SDL_SCANCODE_4;

                case InputKey.Number5:
                    return SDL_Scancode.SDL_SCANCODE_5;

                case InputKey.Number6:
                    return SDL_Scancode.SDL_SCANCODE_6;

                case InputKey.Number7:
                    return SDL_Scancode.SDL_SCANCODE_7;

                case InputKey.Number8:
                    return SDL_Scancode.SDL_SCANCODE_8;

                case InputKey.Number9:
                    return SDL_Scancode.SDL_SCANCODE_9;

                case InputKey.Grave:
                    return SDL_Scancode.SDL_SCANCODE_GRAVE;

                case InputKey.Minus:
                    return SDL_Scancode.SDL_SCANCODE_MINUS;

                case InputKey.Plus:
                    return SDL_Scancode.SDL_SCANCODE_EQUALS;

                case InputKey.BracketLeft:
                    return SDL_Scancode.SDL_SCANCODE_LEFTBRACKET;

                case InputKey.BracketRight:
                    return SDL_Scancode.SDL_SCANCODE_RIGHTBRACKET;

                case InputKey.Semicolon:
                    return SDL_Scancode.SDL_SCANCODE_SEMICOLON;

                case InputKey.Quote:
                    return SDL_Scancode.SDL_SCANCODE_APOSTROPHE;

                case InputKey.Comma:
                    return SDL_Scancode.SDL_SCANCODE_COMMA;

                case InputKey.Period:
                    return SDL_Scancode.SDL_SCANCODE_PERIOD;

                case InputKey.Slash:
                    return SDL_Scancode.SDL_SCANCODE_SLASH;

                case InputKey.BackSlash:
                    return SDL_Scancode.SDL_SCANCODE_BACKSLASH;

                case InputKey.NonUSBackSlash:
                    return SDL_Scancode.SDL_SCANCODE_NONUSBACKSLASH;

                case InputKey.Mute:
                    return SDL_Scancode.SDL_SCANCODE_MUTE;

                case InputKey.PlayPause:
                    return SDL_Scancode.SDL_SCANCODE_MEDIA_PLAY_PAUSE;

                case InputKey.Stop:
                    return SDL_Scancode.SDL_SCANCODE_MEDIA_STOP;

                case InputKey.VolumeUp:
                    return SDL_Scancode.SDL_SCANCODE_VOLUMEUP;

                case InputKey.VolumeDown:
                    return SDL_Scancode.SDL_SCANCODE_VOLUMEDOWN;

                case InputKey.TrackPrevious:
                    return SDL_Scancode.SDL_SCANCODE_MEDIA_PREVIOUS_TRACK;

                case InputKey.TrackNext:
                    return SDL_Scancode.SDL_SCANCODE_MEDIA_NEXT_TRACK;

                case InputKey.LShift:
                    return SDL_Scancode.SDL_SCANCODE_LSHIFT;

                case InputKey.RShift:
                    return SDL_Scancode.SDL_SCANCODE_RSHIFT;

                case InputKey.LControl:
                    return SDL_Scancode.SDL_SCANCODE_LCTRL;

                case InputKey.RControl:
                    return SDL_Scancode.SDL_SCANCODE_RCTRL;

                case InputKey.LAlt:
                    return SDL_Scancode.SDL_SCANCODE_LALT;

                case InputKey.RAlt:
                    return SDL_Scancode.SDL_SCANCODE_RALT;

                case InputKey.LSuper:
                    return SDL_Scancode.SDL_SCANCODE_LGUI;

                case InputKey.RSuper:
                    return SDL_Scancode.SDL_SCANCODE_RGUI;
            }
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
            SDL_GetMasksForPixelFormat(mode.format, &bpp, &unused, &unused, &unused, &unused);
            return new DisplayMode(SDL_GetPixelFormatName(mode.format), new Size(mode.w, mode.h), bpp, mode.refresh_rate, displayIndex);
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
    }
}
