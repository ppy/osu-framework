// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

namespace osu.Framework.Graphics.Containers.Markdown
{
    public interface IMarkdownCodeFlowComponent
    {
        /// <summary>
        /// Creates a <see cref="MarkdownCodeFlowContainer"/> to display text within this <see cref="IMarkdownTextFlowComponent"/>.
        /// </summary>
        /// <remarks>
        /// The <see cref="MarkdownCodeFlowContainer"/> defined by the <see cref="MarkdownCodeFlowContainer"/> is used by default,
        /// but may be overridden via this method to provide additional styling local to this <see cref="IMarkdownTextFlowComponent"/>.
        /// </remarks>
        /// <returns>The <see cref="MarkdownCodeFlowContainer"/>.</returns>
        MarkdownCodeFlowContainer CreateCodeFlow();
    }
}
