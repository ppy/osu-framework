// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using SDL2;

namespace osu.Framework.Platform.Linux.SDL2
{
    public class SDL2Clipboard : Clipboard
    {
        public override string GetText() => SDL.SDL_GetClipboardText();

        public override void SetText(string selectedText) => SDL.SDL_SetClipboardText(selectedText);
    }
}
