// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Platform
{
    public class ClipboardCustomEntry : ClipboardEntry
    {
        public readonly string Format;
        public readonly string Value;

        public ClipboardCustomEntry(string format, string text)
        {
            Format = format;
            Value = text;
        }
    }
}
