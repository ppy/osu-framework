// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using SixLabors.ImageSharp;

namespace osu.Framework.Platform
{
    /// <summary>
    /// Virtual clipboard for use in headless execution.
    /// </summary>
    /// <remarks>
    /// Stores all data in-memory, so the host OS clipboard is not affected.
    /// </remarks>
    public class HeadlessClipboard : Clipboard
    {
        private string? clipboardText;
        private Image? clipboardImage;
        private readonly Dictionary<string, string> customValues = new Dictionary<string, string>();

        public override string? GetText() => clipboardText;

        public override Image<TPixel>? GetImage<TPixel>() => clipboardImage?.CloneAs<TPixel>();

        public override string? GetCustom(string format)
        {
            return customValues[format];
        }

        public override bool SetData(params ClipboardEntry[] entries)
        {
            clipboardText = null;
            clipboardImage = null;
            customValues.Clear();

            foreach (var entry in entries)
            {
                switch (entry)
                {
                    case ClipboardTextEntry textEntry:
                        clipboardText = textEntry.Value;
                        break;

                    case ClipboardImageEntry imageEntry:
                        clipboardImage = imageEntry.Value;
                        break;

                    case ClipboardCustomEntry customEntry:
                        customValues[customEntry.Format] = customEntry.Value;
                        break;
                }
            }

            return true;
        }
    }
}
