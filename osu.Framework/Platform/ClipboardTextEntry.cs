// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Platform
{
    /// <summary>
    /// Represents a plain-text value in the clipboard
    /// </summary>
    public class ClipboardTextEntry : ClipboardEntry
    {
        public readonly string Value;

        public ClipboardTextEntry(string text)
        {
            Value = text;
        }
    }
}
