// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.IO.Stores;

namespace osu.Framework.Text
{
    internal interface IFontStore : ITexturedGlyphLookupStore
    {
        /// <summary>
        /// Searches for a <see cref="IGlyphStore"/> with the specified name.
        /// </summary>
        /// <param name="name">The font name.</param>
        IGlyphStore GetFont(string name);
    }
}
