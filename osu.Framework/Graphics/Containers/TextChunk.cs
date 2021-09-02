// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Sprites;

namespace osu.Framework.Graphics.Containers
{
    internal class TextChunk<T>
        where T : SpriteText
    {
        public readonly string Text;
        public readonly bool NewLineIsParagraph;
        internal readonly Action<T> CreationParameters;

        public TextChunk(string text, bool newLineIsParagraph, Action<T> creationParameters = null)
        {
            Text = text;
            NewLineIsParagraph = newLineIsParagraph;
            CreationParameters = creationParameters;
        }

        public void ApplyParameters(T spriteText)
        {
            CreationParameters?.Invoke(spriteText);
        }
    }
}
