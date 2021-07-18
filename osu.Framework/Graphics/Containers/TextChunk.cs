// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Sprites;

namespace osu.Framework.Graphics.Containers
{
    internal class TextChunk
    {
        public readonly string Text;
        public readonly bool NewLineIsParagraph;
        internal readonly Action<SpriteText> CreationParameters;

        public TextChunk(string text, bool newLineIsParagraph, Action<SpriteText> creationParameters = null)
        {
            Text = text;
            NewLineIsParagraph = newLineIsParagraph;
            CreationParameters = creationParameters;
        }

        public void ApplyParameters(SpriteText spriteText)
        {
            CreationParameters?.Invoke(spriteText);
        }
    }
}
