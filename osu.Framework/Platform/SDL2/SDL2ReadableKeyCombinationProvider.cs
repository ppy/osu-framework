// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Input;
using osu.Framework.Input.Bindings;
using SDL2;

namespace osu.Framework.Platform.SDL2
{
    public class SDL2ReadableKeyCombinationProvider : ReadableKeyCombinationProvider
    {
        protected override string GetReadableKey(InputKey key)
        {
            if (!changeableByKeyboardLayout(key))
                return base.GetReadableKey(key);

            var keycode = SDL.SDL_GetKeyFromScancode(toScancode(key));

            switch (keycode)
            {
                case SDL.SDL_Keycode.SDLK_MINUS:
                    return "Minus";

                case SDL.SDL_Keycode.SDLK_PLUS:
                    return "Plus";
            }

            var keyname = SDL.SDL_GetKeyName(keycode);

            if (string.IsNullOrEmpty(keyname))
                return base.GetReadableKey(key);

            return keyname.ToUpper();
        }

        private bool changeableByKeyboardLayout(InputKey key)
        {
            return key >= InputKey.A && key < InputKey.LastKey;
        }

        private SDL.SDL_Scancode toScancode(InputKey key)
        {
            switch (key)
            {
                default:
                    return SDL.SDL_Scancode.SDL_SCANCODE_UNKNOWN;

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
            }
        }
    }
}
