// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Input.Bindings;
using osu.Framework.Platform.SDL2;
using SDL2;

namespace osu.Framework.Platform.Windows
{
    public class WindowsReadableKeyCombinationProvider : SDL2ReadableKeyCombinationProvider
    {
        protected override string GetReadableKey(InputKey key)
        {
            switch (key)
            {
                case InputKey.Super:
                    return "Win";

                default:
                    return base.GetReadableKey(key);
            }
        }

        protected override bool TryGetNameFromKeycode(SDL.SDL_Keycode keycode, out string name)
        {
            switch (keycode)
            {
                case SDL.SDL_Keycode.SDLK_LGUI:
                    name = "LWin";
                    return true;

                case SDL.SDL_Keycode.SDLK_RGUI:
                    name = "RWin";
                    return true;

                default:
                    return base.TryGetNameFromKeycode(keycode, out name);
            }
        }
    }
}
