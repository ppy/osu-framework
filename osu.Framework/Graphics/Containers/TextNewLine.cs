// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Extensions.IEnumerableExtensions;

namespace osu.Framework.Graphics.Containers
{
    public class TextNewLine : TextPart
    {
        private readonly bool indicatesNewParagraph;

        public TextNewLine(bool indicatesNewParagraph)
        {
            this.indicatesNewParagraph = indicatesNewParagraph;
        }

        protected override IEnumerable<Drawable> CreateDrawablesFor(TextFlowContainer textFlowContainer)
        {
            var newLineContainer = new TextFlowContainer.NewLineContainer(indicatesNewParagraph);
            return newLineContainer.Yield();
        }
    }
}
