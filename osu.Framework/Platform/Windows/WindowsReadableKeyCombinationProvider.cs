// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Input.Bindings;
using osu.Framework.Platform.SDL3;
using SDL;

namespace osu.Framework.Platform.Windows
{
    public class WindowsReadableKeyCombinationProvider : SDL3ReadableKeyCombinationProvider
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

        protected override bool TryGetNameFromKeycode(SDL_Keycode keycode, out string name)
        {
            switch (keycode)
            {
                case SDL_Keycode.SDLK_LGUI:
                    name = "LWin";
                    return true;

                case SDL_Keycode.SDLK_RGUI:
                    name = "RWin";
                    return true;

                default:
                    return base.TryGetNameFromKeycode(keycode, out name);
            }
        }
    }
}
