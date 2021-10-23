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

            var keycode = SDL.SDL_GetKeyFromScancode(key.ToScancode());

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
    }
}
