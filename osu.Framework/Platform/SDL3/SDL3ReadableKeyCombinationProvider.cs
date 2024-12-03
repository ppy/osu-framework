// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Globalization;
using osu.Framework.Input;
using osu.Framework.Input.Bindings;
using SDL;
using static SDL.SDL3;

namespace osu.Framework.Platform.SDL3
{
    public class SDL3ReadableKeyCombinationProvider : ReadableKeyCombinationProvider
    {
        private static SDL_Keycode getKeyFromScancode(SDL_Scancode scancode, SDL_Keymod modstate)
        {
            if (FrameworkEnvironment.UseSDL3)
                return SDL_GetKeyFromScancode(scancode, modstate, false); // third parameter is not useful unless SDL_HINT_KEYCODE_OPTIONS is set

            return (SDL_Keycode)global::SDL2.SDL.SDL_GetKeyFromScancode((global::SDL2.SDL.SDL_Scancode)scancode);
        }

        private static string? getKeyName(SDL_Keycode keycode)
        {
            if (FrameworkEnvironment.UseSDL3)
                return SDL_GetKeyName(keycode);

            return global::SDL2.SDL.SDL_GetKeyName((global::SDL2.SDL.SDL_Keycode)keycode);
        }

        protected override string GetReadableKey(InputKey key)
        {
            // In SDL3, SDL_GetKeyFromScancode may return a different keycode depending on key modifier. Use NONE to keep consistency with SDL2 for now.
            var keycode = getKeyFromScancode(key.ToScancode(), SDL_KMOD_NONE);

            // early return if unknown. probably because key isn't a keyboard key, or doesn't map to an `SDL_Scancode`.
            if (keycode == SDL_Keycode.SDLK_UNKNOWN)
                return base.GetReadableKey(key);

            string? name;

            // overrides for some keys that we want displayed differently from SDL_GetKeyName().
            if (TryGetNameFromKeycode(keycode, out name))
                return name;

            name = getKeyName(keycode);

            // fall back if SDL couldn't find a name.
            if (string.IsNullOrEmpty(name))
                return base.GetReadableKey(key);

            // true if SDL_GetKeyName() returned a proper key/scancode name.
            // see https://github.com/libsdl-org/SDL/blob/release-2.0.16/src/events/SDL_keyboard.c#L1012
            if (((int)keycode & SDLK_SCANCODE_MASK) != 0)
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
        protected virtual bool TryGetNameFromKeycode(SDL_Keycode keycode, out string name)
        {
            switch (keycode)
            {
                case SDL_Keycode.SDLK_RETURN:
                    name = "Enter";
                    return true;

                case SDL_Keycode.SDLK_ESCAPE:
                    name = "Esc";
                    return true;

                case SDL_Keycode.SDLK_BACKSPACE:
                    name = "Backsp";
                    return true;

                case SDL_Keycode.SDLK_TAB:
                    name = "Tab";
                    return true;

                case SDL_Keycode.SDLK_SPACE:
                    name = "Space";
                    return true;

                case SDL_Keycode.SDLK_PLUS:
                    name = "Plus";
                    return true;

                case SDL_Keycode.SDLK_MINUS:
                    name = "Minus";
                    return true;

                case SDL_Keycode.SDLK_DELETE:
                    name = "Del";
                    return true;

                case SDL_Keycode.SDLK_CAPSLOCK:
                    name = "Caps";
                    return true;

                case SDL_Keycode.SDLK_INSERT:
                    name = "Ins";
                    return true;

                case SDL_Keycode.SDLK_PAGEUP:
                    name = "PgUp";
                    return true;

                case SDL_Keycode.SDLK_PAGEDOWN:
                    name = "PgDn";
                    return true;

                case SDL_Keycode.SDLK_NUMLOCKCLEAR:
                    name = "NumLock";
                    return true;

                case SDL_Keycode.SDLK_KP_DIVIDE:
                    name = "NumpadDivide";
                    return true;

                case SDL_Keycode.SDLK_KP_MULTIPLY:
                    name = "NumpadMultiply";
                    return true;

                case SDL_Keycode.SDLK_KP_MINUS:
                    name = "NumpadMinus";
                    return true;

                case SDL_Keycode.SDLK_KP_PLUS:
                    name = "NumpadPlus";
                    return true;

                case SDL_Keycode.SDLK_KP_ENTER:
                    name = "NumpadEnter";
                    return true;

                case SDL_Keycode.SDLK_KP_PERIOD:
                    name = "NumpadDecimal";
                    return true;

                case SDL_Keycode.SDLK_KP_0:
                    name = "Numpad0";
                    return true;

                case SDL_Keycode.SDLK_KP_1:
                    name = "Numpad1";
                    return true;

                case SDL_Keycode.SDLK_KP_2:
                    name = "Numpad2";
                    return true;

                case SDL_Keycode.SDLK_KP_3:
                    name = "Numpad3";
                    return true;

                case SDL_Keycode.SDLK_KP_4:
                    name = "Numpad4";
                    return true;

                case SDL_Keycode.SDLK_KP_5:
                    name = "Numpad5";
                    return true;

                case SDL_Keycode.SDLK_KP_6:
                    name = "Numpad6";
                    return true;

                case SDL_Keycode.SDLK_KP_7:
                    name = "Numpad7";
                    return true;

                case SDL_Keycode.SDLK_KP_8:
                    name = "Numpad8";
                    return true;

                case SDL_Keycode.SDLK_KP_9:
                    name = "Numpad9";
                    return true;

                case SDL_Keycode.SDLK_LCTRL:
                    name = "LCtrl";
                    return true;

                case SDL_Keycode.SDLK_LSHIFT:
                    name = "LShift";
                    return true;

                case SDL_Keycode.SDLK_LALT:
                    name = "LAlt";
                    return true;

                case SDL_Keycode.SDLK_RCTRL:
                    name = "RCtrl";
                    return true;

                case SDL_Keycode.SDLK_RSHIFT:
                    name = "RShift";
                    return true;

                case SDL_Keycode.SDLK_RALT:
                    name = "RAlt";
                    return true;

                case SDL_Keycode.SDLK_VOLUMEUP:
                    name = "Vol. Up";
                    return true;

                case SDL_Keycode.SDLK_VOLUMEDOWN:
                    name = "Vol. Down";
                    return true;

                case SDL_Keycode.SDLK_MEDIA_NEXT_TRACK:
                    name = "Media Next";
                    return true;

                case SDL_Keycode.SDLK_MEDIA_PREVIOUS_TRACK:
                    name = "Media Previous";
                    return true;

                case SDL_Keycode.SDLK_MEDIA_STOP:
                    name = "Media Stop";
                    return true;

                case SDL_Keycode.SDLK_MEDIA_PLAY:
                    name = "Media Play";
                    return true;

                case SDL_Keycode.SDLK_MUTE:
                    name = "Mute";
                    return true;

                default:
                    name = string.Empty;
                    return false;
            }
        }
    }
}
