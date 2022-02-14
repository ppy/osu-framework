// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Platform
{
    public abstract class Clipboard
    {
        public abstract string GetText();

        public abstract void SetText(string selectedText);
    }
}
