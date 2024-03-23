// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using SDL2;
using SixLabors.ImageSharp;

namespace osu.Framework.Platform.SDL2
{
    public class SDL2Clipboard : Clipboard
    {
        private readonly Dictionary<string, string> customFormatValues = new Dictionary<string, string>();

        // SDL cannot differentiate between string.Empty and no text (eg. empty clipboard or an image)
        // doesn't matter as text editors don't really allow copying empty strings.
        // assume that empty text means no text.
        public override string? GetText() => SDL.SDL_HasClipboardText() == SDL.SDL_bool.SDL_TRUE ? SDL.SDL_GetClipboardText() : null;

        public override Image<TPixel>? GetImage<TPixel>()
        {
            return null;
        }

        public override string? GetCustom(string format)
        {
            return customFormatValues[format];
        }

        public override bool SetData(params ClipboardEntry[] entries)
        {
            customFormatValues.Clear();

            if (entries.Any(e => e is ClipboardImageEntry))
            {
                return false;
            }

            foreach (var entry in entries)
            {
                switch (entry)
                {
                    case ClipboardTextEntry textEntry:
                        SDL.SDL_SetClipboardText(textEntry.Value);
                        break;

                    case ClipboardCustomEntry customEntry:
                        customFormatValues[customEntry.Format] = customEntry.Value;
                        break;
                }
            }

            return true;
        }
    }
}
