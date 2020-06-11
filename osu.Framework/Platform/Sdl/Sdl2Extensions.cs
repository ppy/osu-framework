// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osuTK.Input;
using SDL2;

namespace osu.Framework.Platform.Sdl
{
    public static class Sdl2Extensions
    {
        public static Key ToKey(this SDL.SDL_Keysym sdlKeysym)
        {
            switch (sdlKeysym.sym)
            {
                default:
                case SDL.SDL_Keycode.SDLK_UNKNOWN:
                    return Key.Unknown;

                case SDL.SDL_Keycode.SDLK_KP_COMMA:
                    return Key.Comma;

                case SDL.SDL_Keycode.SDLK_KP_TAB:
                    return Key.Tab;

                case SDL.SDL_Keycode.SDLK_KP_BACKSPACE:
                    return Key.BackSpace;

                case SDL.SDL_Keycode.SDLK_KP_A:
                    return Key.A;

                case SDL.SDL_Keycode.SDLK_KP_B:
                    return Key.B;

                case SDL.SDL_Keycode.SDLK_KP_C:
                    return Key.C;

                case SDL.SDL_Keycode.SDLK_KP_D:
                    return Key.D;

                case SDL.SDL_Keycode.SDLK_KP_E:
                    return Key.E;

                case SDL.SDL_Keycode.SDLK_KP_F:
                    return Key.F;

                case SDL.SDL_Keycode.SDLK_KP_SPACE:
                    return Key.Space;

                case SDL.SDL_Keycode.SDLK_KP_CLEAR:
                    return Key.Clear;

                case SDL.SDL_Keycode.SDLK_RETURN:
                    return Key.Enter;

                case SDL.SDL_Keycode.SDLK_ESCAPE:
                    return Key.Escape;

                case SDL.SDL_Keycode.SDLK_BACKSPACE:
                    return Key.BackSpace;

                case SDL.SDL_Keycode.SDLK_TAB:
                    return Key.Tab;

                case SDL.SDL_Keycode.SDLK_SPACE:
                    return Key.Space;

                case SDL.SDL_Keycode.SDLK_QUOTE:
                    return Key.Quote;

                case SDL.SDL_Keycode.SDLK_COMMA:
                    return Key.Comma;

                case SDL.SDL_Keycode.SDLK_MINUS:
                    return Key.Minus;

                case SDL.SDL_Keycode.SDLK_PERIOD:
                    return Key.Period;

                case SDL.SDL_Keycode.SDLK_SLASH:
                    return Key.Slash;

                case SDL.SDL_Keycode.SDLK_0:
                    return Key.Number0;

                case SDL.SDL_Keycode.SDLK_1:
                    return Key.Number1;

                case SDL.SDL_Keycode.SDLK_2:
                    return Key.Number2;

                case SDL.SDL_Keycode.SDLK_3:
                    return Key.Number3;

                case SDL.SDL_Keycode.SDLK_4:
                    return Key.Number4;

                case SDL.SDL_Keycode.SDLK_5:
                    return Key.Number5;

                case SDL.SDL_Keycode.SDLK_6:
                    return Key.Number6;

                case SDL.SDL_Keycode.SDLK_7:
                    return Key.Number7;

                case SDL.SDL_Keycode.SDLK_8:
                    return Key.Number8;

                case SDL.SDL_Keycode.SDLK_9:
                    return Key.Number9;

                case SDL.SDL_Keycode.SDLK_SEMICOLON:
                    return Key.Semicolon;

                case SDL.SDL_Keycode.SDLK_EQUALS:
                    return Key.Plus;

                case SDL.SDL_Keycode.SDLK_LEFTBRACKET:
                    return Key.BracketLeft;

                case SDL.SDL_Keycode.SDLK_BACKSLASH:
                    return Key.BackSlash;

                case SDL.SDL_Keycode.SDLK_RIGHTBRACKET:
                    return Key.BracketRight;

                case SDL.SDL_Keycode.SDLK_BACKQUOTE:
                    return Key.Tilde;

                case SDL.SDL_Keycode.SDLK_a:
                    return Key.A;

                case SDL.SDL_Keycode.SDLK_b:
                    return Key.B;

                case SDL.SDL_Keycode.SDLK_c:
                    return Key.C;

                case SDL.SDL_Keycode.SDLK_d:
                    return Key.D;

                case SDL.SDL_Keycode.SDLK_e:
                    return Key.E;

                case SDL.SDL_Keycode.SDLK_f:
                    return Key.F;

                case SDL.SDL_Keycode.SDLK_g:
                    return Key.G;

                case SDL.SDL_Keycode.SDLK_h:
                    return Key.H;

                case SDL.SDL_Keycode.SDLK_i:
                    return Key.I;

                case SDL.SDL_Keycode.SDLK_j:
                    return Key.J;

                case SDL.SDL_Keycode.SDLK_k:
                    return Key.K;

                case SDL.SDL_Keycode.SDLK_l:
                    return Key.L;

                case SDL.SDL_Keycode.SDLK_m:
                    return Key.M;

                case SDL.SDL_Keycode.SDLK_n:
                    return Key.N;

                case SDL.SDL_Keycode.SDLK_o:
                    return Key.O;

                case SDL.SDL_Keycode.SDLK_p:
                    return Key.P;

                case SDL.SDL_Keycode.SDLK_q:
                    return Key.Q;

                case SDL.SDL_Keycode.SDLK_r:
                    return Key.R;

                case SDL.SDL_Keycode.SDLK_s:
                    return Key.S;

                case SDL.SDL_Keycode.SDLK_t:
                    return Key.T;

                case SDL.SDL_Keycode.SDLK_u:
                    return Key.U;

                case SDL.SDL_Keycode.SDLK_v:
                    return Key.V;

                case SDL.SDL_Keycode.SDLK_w:
                    return Key.W;

                case SDL.SDL_Keycode.SDLK_x:
                    return Key.X;

                case SDL.SDL_Keycode.SDLK_y:
                    return Key.Y;

                case SDL.SDL_Keycode.SDLK_z:
                    return Key.Z;

                case SDL.SDL_Keycode.SDLK_CAPSLOCK:
                    return Key.CapsLock;

                case SDL.SDL_Keycode.SDLK_F1:
                    return Key.F1;

                case SDL.SDL_Keycode.SDLK_F2:
                    return Key.F2;

                case SDL.SDL_Keycode.SDLK_F3:
                    return Key.F3;

                case SDL.SDL_Keycode.SDLK_F4:
                    return Key.F4;

                case SDL.SDL_Keycode.SDLK_F5:
                    return Key.F5;

                case SDL.SDL_Keycode.SDLK_F6:
                    return Key.F6;

                case SDL.SDL_Keycode.SDLK_F7:
                    return Key.F7;

                case SDL.SDL_Keycode.SDLK_F8:
                    return Key.F8;

                case SDL.SDL_Keycode.SDLK_F9:
                    return Key.F9;

                case SDL.SDL_Keycode.SDLK_F10:
                    return Key.F10;

                case SDL.SDL_Keycode.SDLK_F11:
                    return Key.F11;

                case SDL.SDL_Keycode.SDLK_F12:
                    return Key.F12;

                case SDL.SDL_Keycode.SDLK_PRINTSCREEN:
                    return Key.PrintScreen;

                case SDL.SDL_Keycode.SDLK_SCROLLLOCK:
                    return Key.ScrollLock;

                case SDL.SDL_Keycode.SDLK_PAUSE:
                    return Key.Pause;

                case SDL.SDL_Keycode.SDLK_INSERT:
                    return Key.Insert;

                case SDL.SDL_Keycode.SDLK_HOME:
                    return Key.Home;

                case SDL.SDL_Keycode.SDLK_PAGEUP:
                    return Key.PageUp;

                case SDL.SDL_Keycode.SDLK_DELETE:
                    return Key.Delete;

                case SDL.SDL_Keycode.SDLK_END:
                    return Key.End;

                case SDL.SDL_Keycode.SDLK_PAGEDOWN:
                    return Key.PageDown;

                case SDL.SDL_Keycode.SDLK_RIGHT:
                    return Key.Right;

                case SDL.SDL_Keycode.SDLK_LEFT:
                    return Key.Left;

                case SDL.SDL_Keycode.SDLK_DOWN:
                    return Key.Down;

                case SDL.SDL_Keycode.SDLK_UP:
                    return Key.Up;

                case SDL.SDL_Keycode.SDLK_NUMLOCKCLEAR:
                    return Key.NumLock;

                case SDL.SDL_Keycode.SDLK_KP_DIVIDE:
                    return Key.KeypadDivide;

                case SDL.SDL_Keycode.SDLK_KP_MULTIPLY:
                    return Key.KeypadMultiply;

                case SDL.SDL_Keycode.SDLK_KP_MINUS:
                    return Key.KeypadMinus;

                case SDL.SDL_Keycode.SDLK_KP_PLUS:
                    return Key.KeypadPlus;

                case SDL.SDL_Keycode.SDLK_KP_ENTER:
                    return Key.KeypadEnter;

                case SDL.SDL_Keycode.SDLK_KP_1:
                    return Key.Keypad1;

                case SDL.SDL_Keycode.SDLK_KP_2:
                    return Key.Keypad2;

                case SDL.SDL_Keycode.SDLK_KP_3:
                    return Key.Keypad3;

                case SDL.SDL_Keycode.SDLK_KP_4:
                    return Key.Keypad4;

                case SDL.SDL_Keycode.SDLK_KP_5:
                    return Key.Keypad5;

                case SDL.SDL_Keycode.SDLK_KP_6:
                    return Key.Keypad6;

                case SDL.SDL_Keycode.SDLK_KP_7:
                    return Key.Keypad7;

                case SDL.SDL_Keycode.SDLK_KP_8:
                    return Key.Keypad8;

                case SDL.SDL_Keycode.SDLK_KP_9:
                    return Key.Keypad9;

                case SDL.SDL_Keycode.SDLK_KP_0:
                    return Key.Keypad0;

                case SDL.SDL_Keycode.SDLK_KP_PERIOD:
                    return Key.KeypadPeriod;

                case SDL.SDL_Keycode.SDLK_F13:
                    return Key.F13;

                case SDL.SDL_Keycode.SDLK_F14:
                    return Key.F14;

                case SDL.SDL_Keycode.SDLK_F15:
                    return Key.F15;

                case SDL.SDL_Keycode.SDLK_F16:
                    return Key.F16;

                case SDL.SDL_Keycode.SDLK_F17:
                    return Key.F17;

                case SDL.SDL_Keycode.SDLK_F18:
                    return Key.F18;

                case SDL.SDL_Keycode.SDLK_F19:
                    return Key.F19;

                case SDL.SDL_Keycode.SDLK_F20:
                    return Key.F20;

                case SDL.SDL_Keycode.SDLK_F21:
                    return Key.F21;

                case SDL.SDL_Keycode.SDLK_F22:
                    return Key.F22;

                case SDL.SDL_Keycode.SDLK_F23:
                    return Key.F23;

                case SDL.SDL_Keycode.SDLK_F24:
                    return Key.F24;

                case SDL.SDL_Keycode.SDLK_MENU:
                    return Key.Menu;

                case SDL.SDL_Keycode.SDLK_STOP:
                    return Key.Stop;

                case SDL.SDL_Keycode.SDLK_MUTE:
                    return Key.Mute;

                case SDL.SDL_Keycode.SDLK_VOLUMEUP:
                    return Key.VolumeUp;

                case SDL.SDL_Keycode.SDLK_VOLUMEDOWN:
                    return Key.VolumeDown;

                case SDL.SDL_Keycode.SDLK_CLEAR:
                    return Key.Clear;

                case SDL.SDL_Keycode.SDLK_DECIMALSEPARATOR:
                    return Key.KeypadDecimal;

                case SDL.SDL_Keycode.SDLK_LCTRL:
                    return Key.ControlLeft;

                case SDL.SDL_Keycode.SDLK_LSHIFT:
                    return Key.ShiftLeft;

                case SDL.SDL_Keycode.SDLK_LALT:
                    return Key.AltLeft;

                case SDL.SDL_Keycode.SDLK_LGUI:
                    return Key.WinLeft;

                case SDL.SDL_Keycode.SDLK_RCTRL:
                    return Key.ControlRight;

                case SDL.SDL_Keycode.SDLK_RSHIFT:
                    return Key.ShiftRight;

                case SDL.SDL_Keycode.SDLK_RALT:
                    return Key.AltRight;

                case SDL.SDL_Keycode.SDLK_RGUI:
                    return Key.WinRight;

                case SDL.SDL_Keycode.SDLK_AUDIONEXT:
                    return Key.TrackNext;

                case SDL.SDL_Keycode.SDLK_AUDIOPREV:
                    return Key.TrackPrevious;

                case SDL.SDL_Keycode.SDLK_AUDIOSTOP:
                    return Key.Stop;

                case SDL.SDL_Keycode.SDLK_AUDIOPLAY:
                    return Key.PlayPause;

                case SDL.SDL_Keycode.SDLK_AUDIOMUTE:
                    return Key.Mute;

                case SDL.SDL_Keycode.SDLK_SLEEP:
                    return Key.Sleep;
            }
        }

        public static WindowState ToWindowState(this SDL.SDL_WindowFlags windowFlags)
        {
            if (windowFlags.HasFlag(SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN_DESKTOP) ||
                windowFlags.HasFlag(SDL.SDL_WindowFlags.SDL_WINDOW_BORDERLESS) && windowFlags.HasFlag(SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN))
                return WindowState.FullscreenBorderless;

            if (windowFlags.HasFlag(SDL.SDL_WindowFlags.SDL_WINDOW_MINIMIZED))
                return WindowState.Minimised;

            if (windowFlags.HasFlag(SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN))
                return WindowState.Fullscreen;

            if (windowFlags.HasFlag(SDL.SDL_WindowFlags.SDL_WINDOW_MAXIMIZED))
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
    }
}
