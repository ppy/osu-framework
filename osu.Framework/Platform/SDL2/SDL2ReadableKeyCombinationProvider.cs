// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Globalization;
using osu.Framework.Input;
using osu.Framework.Input.Bindings;
using SDL2;

namespace osu.Framework.Platform.SDL2
{
    public class SDL2ReadableKeyCombinationProvider : ReadableKeyCombinationProvider
    {
        protected override string GetReadableKey(InputKey key)
        {
            var keycode = SDL.SDL_GetKeyFromScancode(key.ToScancode());

            // early return if unknown. probably because key isn't a keyboard key, or doesn't map to an `SDL_Scancode`.
            if (keycode == SDL.SDL_Keycode.SDLK_UNKNOWN)
                return base.GetReadableKey(key);

            string name;

            // overrides for some keys that we want displayed differently from SDL_GetKeyName().
            if (TryGetNameFromKeycode(keycode, out name))
                return name;

            name = SDL.SDL_GetKeyName(keycode);

            // fall back if SDL couldn't find a name.
            if (string.IsNullOrEmpty(name))
                return base.GetReadableKey(key);

            // true if SDL_GetKeyName() returned a proper key/scancode name.
            // see https://github.com/libsdl-org/SDL/blob/release-2.0.16/src/events/SDL_keyboard.c#L1012
            if (keycode.HasScancodeMask())
                return name;

            // SDL_GetKeyName() returned a unicode character that would be produced if that key was pressed.
            // consumers expect an uppercase letter.
            // `.ToUpper()` with current culture may be slightly inaccurate if the framework locale
            // is different from the locale of the keyboard layout being used,
            // but we have no means to detect this anyway, as SDL doesn't provide the name of the layout.
            return name.ToUpper(CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// Provides overrides for some keys that we want displayed differently from SDL_GetKeyName().
        /// </summary>
        /// <remarks>
        /// Should be overriden per-platform to provide platform-specific names for applicable keys.
        /// </remarks>
        protected virtual bool TryGetNameFromKeycode(SDL.SDL_Keycode keycode, out string name)
        {
            switch (keycode)
            {
                case SDL.SDL_Keycode.SDLK_RETURN:
                    name = "Enter";
                    return true;

                case SDL.SDL_Keycode.SDLK_ESCAPE:
                    name = "Esc";
                    return true;

                case SDL.SDL_Keycode.SDLK_BACKSPACE:
                    name = "Backsp";
                    return true;

                case SDL.SDL_Keycode.SDLK_TAB:
                    name = "Tab";
                    return true;

                case SDL.SDL_Keycode.SDLK_SPACE:
                    name = "Space";
                    return true;

                case SDL.SDL_Keycode.SDLK_PLUS:
                    name = "Plus";
                    return true;

                case SDL.SDL_Keycode.SDLK_MINUS:
                    name = "Minus";
                    return true;

                case SDL.SDL_Keycode.SDLK_DELETE:
                    name = "Del";
                    return true;

                case SDL.SDL_Keycode.SDLK_CAPSLOCK:
                    name = "Caps";
                    return true;

                case SDL.SDL_Keycode.SDLK_INSERT:
                    name = "Ins";
                    return true;

                case SDL.SDL_Keycode.SDLK_PAGEUP:
                    name = "PgUp";
                    return true;

                case SDL.SDL_Keycode.SDLK_PAGEDOWN:
                    name = "PgDn";
                    return true;

                case SDL.SDL_Keycode.SDLK_NUMLOCKCLEAR:
                    name = "NumLock";
                    return true;

                case SDL.SDL_Keycode.SDLK_KP_DIVIDE:
                    name = "NumpadDivide";
                    return true;

                case SDL.SDL_Keycode.SDLK_KP_MULTIPLY:
                    name = "NumpadMultiply";
                    return true;

                case SDL.SDL_Keycode.SDLK_KP_MINUS:
                    name = "NumpadMinus";
                    return true;

                case SDL.SDL_Keycode.SDLK_KP_PLUS:
                    name = "NumpadPlus";
                    return true;

                case SDL.SDL_Keycode.SDLK_KP_ENTER:
                    name = "NumpadEnter";
                    return true;

                case SDL.SDL_Keycode.SDLK_KP_PERIOD:
                    name = "NumpadDecimal";
                    return true;

                case SDL.SDL_Keycode.SDLK_KP_0:
                    name = "Numpad0";
                    return true;

                case SDL.SDL_Keycode.SDLK_KP_1:
                    name = "Numpad1";
                    return true;

                case SDL.SDL_Keycode.SDLK_KP_2:
                    name = "Numpad2";
                    return true;

                case SDL.SDL_Keycode.SDLK_KP_3:
                    name = "Numpad3";
                    return true;

                case SDL.SDL_Keycode.SDLK_KP_4:
                    name = "Numpad4";
                    return true;

                case SDL.SDL_Keycode.SDLK_KP_5:
                    name = "Numpad5";
                    return true;

                case SDL.SDL_Keycode.SDLK_KP_6:
                    name = "Numpad6";
                    return true;

                case SDL.SDL_Keycode.SDLK_KP_7:
                    name = "Numpad7";
                    return true;

                case SDL.SDL_Keycode.SDLK_KP_8:
                    name = "Numpad8";
                    return true;

                case SDL.SDL_Keycode.SDLK_KP_9:
                    name = "Numpad9";
                    return true;

                case SDL.SDL_Keycode.SDLK_LCTRL:
                    name = "LCtrl";
                    return true;

                case SDL.SDL_Keycode.SDLK_LSHIFT:
                    name = "LShift";
                    return true;

                case SDL.SDL_Keycode.SDLK_LALT:
                    name = "LAlt";
                    return true;

                case SDL.SDL_Keycode.SDLK_RCTRL:
                    name = "RCtrl";
                    return true;

                case SDL.SDL_Keycode.SDLK_RSHIFT:
                    name = "RShift";
                    return true;

                case SDL.SDL_Keycode.SDLK_RALT:
                    name = "RAlt";
                    return true;

                case SDL.SDL_Keycode.SDLK_VOLUMEUP:
                    name = "Vol. Up";
                    return true;

                case SDL.SDL_Keycode.SDLK_VOLUMEDOWN:
                    name = "Vol. Down";
                    return true;

                case SDL.SDL_Keycode.SDLK_AUDIONEXT:
                    name = "Media Next";
                    return true;

                case SDL.SDL_Keycode.SDLK_AUDIOPREV:
                    name = "Media Previous";
                    return true;

                case SDL.SDL_Keycode.SDLK_AUDIOSTOP:
                    name = "Media Stop";
                    return true;

                case SDL.SDL_Keycode.SDLK_AUDIOPLAY:
                    name = "Media Play";
                    return true;

                case SDL.SDL_Keycode.SDLK_AUDIOMUTE:
                    name = "Mute";
                    return true;

                default:
                    name = string.Empty;
                    return false;
            }
        }
    }
}
