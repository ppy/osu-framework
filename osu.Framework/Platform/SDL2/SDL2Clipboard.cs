// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using SDL;
using SixLabors.ImageSharp;

namespace osu.Framework.Platform.SDL2
{
    public class SDL2Clipboard : Clipboard
    {
        // SDL cannot differentiate between string.Empty and no text (eg. empty clipboard or an image)
        // doesn't matter as text editors don't really allow copying empty strings.
        // assume that empty text means no text.
        public override string? GetText() => SDL3.SDL_HasClipboardText() == SDL_bool.SDL_TRUE ? SDL3.SDL_GetClipboardText() : null;

        public override void SetText(string text) => SDL3.SDL_SetClipboardText(text);

        public override Image<TPixel>? GetImage<TPixel>()
        {
            return null;
        }

        public override bool SetImage(Image image)
        {
            return false;
        }
    }
}
