// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Input.Bindings;
using osu.Framework.Platform.SDL2;
using SDL2;

namespace osu.Framework.Platform.MacOS
{
    public class MacOSReadableKeyCombinationProvider : SDL2ReadableKeyCombinationProvider
    {
        protected override bool TryGetNameFromKeycode(SDL.SDL_Keycode keycode, out string name)
        {
            switch (keycode)
            {
                case SDL.SDL_Keycode.SDLK_LGUI:
                    name = "LCmd";
                    return true;

                case SDL.SDL_Keycode.SDLK_RGUI:
                    name = "RCmd";
                    return true;

                case SDL.SDL_Keycode.SDLK_LALT:
                    name = "LOption";
                    return true;

                case SDL.SDL_Keycode.SDLK_RALT:
                    name = "ROption";
                    return true;

                default:
                    return base.TryGetNameFromKeycode(keycode, out name);
            }
        }
    }
}
