// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

namespace osu.Framework.Graphics.Containers.Markdown
{
    public interface IMarkdownTextFlowComponent
    {
        /// <summary>
        /// Creates a <see cref="MarkdownTextFlowContainer"/> to display text within this <see cref="IMarkdownTextFlowComponent"/>.
        /// </summary>
        /// <remarks>
        /// The <see cref="MarkdownTextFlowContainer"/> defined by the <see cref="MarkdownContainer"/> is used by default,
        /// but may be overridden via this method to provide additional styling local to this <see cref="IMarkdownTextFlowComponent"/>.
        /// </remarks>
        /// <returns>The <see cref="MarkdownTextFlowContainer"/>.</returns>
        MarkdownTextFlowContainer CreateTextFlow();
    }
}
