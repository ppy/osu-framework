// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Extensions.EnumExtensions;
using osu.Framework.Input;
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
                    return Key.Keypad5;

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
    }
}
