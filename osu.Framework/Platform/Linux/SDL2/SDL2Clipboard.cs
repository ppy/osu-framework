// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using SDL2;
using SixLabors.ImageSharp;

namespace osu.Framework.Platform.Linux.SDL2
{
    public class SDL2Clipboard : Clipboard
    {
        public override string GetText() => SDL.SDL_GetClipboardText();

        public override void SetText(string selectedText) => SDL.SDL_SetClipboardText(selectedText);

        public override Image<TPixel> GetImage<TPixel>()
        {
            return null;
        }

        public override bool SetImage(Image image)
        {
            return false;
        }
    }
}
