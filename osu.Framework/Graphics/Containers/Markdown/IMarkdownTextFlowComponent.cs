// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;

namespace osu.Framework.Graphics.Containers.Markdown
{
    [Cached(Type = typeof(IMarkdownTextFlowComponent))]
    public interface IMarkdownTextFlowComponent
    {
        /// <summary>
        /// Creates a <see cref="MarkdownTextFlowContainer"/> to display text within this <see cref="IMarkdownTextFlowComponent"/>.
        /// </summary>
        /// <remarks>
        /// The <see cref="MarkdownTextFlowContainer"/> defined by the <see cref="IMarkdownTextFlowComponent"/> resolved from dependencies is used by default,
        /// but may be overridden via this method to provide additional styling local to this <see cref="IMarkdownTextFlowComponent"/>.
        /// </remarks>
        /// <returns>The <see cref="MarkdownTextFlowContainer"/>.</returns>
        MarkdownTextFlowContainer CreateTextFlow();
    }
}
