// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using SixLabors.ImageSharp;

namespace osu.Framework.Platform
{
    /// <summary>
    /// Represents an image value in the clipboard
    /// </summary>
    public class ClipboardImageEntry : ClipboardEntry
    {
        public readonly Image Value;

        public ClipboardImageEntry(Image text)
        {
            Value = text;
        }
    }
}
