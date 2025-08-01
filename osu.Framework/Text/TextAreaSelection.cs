// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.UserInterface;

namespace osu.Framework.Text
{
    public class TextAreaSelection : TextSelection
    {
        private readonly TextAreaTextLayout layout;

        public TextAreaSelection(Func<string> getText, TextAreaTextLayout layout)
            : base(getText)
        {
            this.layout = layout;
        }

        public override int GetLineStart(int position)
        {
            var textPosition = layout.IndexToTextPosition(position);

            return layout.TextPositionToIndex(textPosition with { Column = 0 });
        }

        public override int GetLineEnd(int position)
        {
            var textPosition = layout.IndexToTextPosition(position);

            if (layout.TryGetLineInfo(textPosition.Row, out var line))
                return layout.TextPositionToIndex(textPosition with { Column = line.CharacterCount - 1 });

            return position;
        }
    }
}
