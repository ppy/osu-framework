// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Graphics.Cursor
{
    /// <summary>
    /// Implementing this interface allows the implementing <see cref="Drawable"/> to display a custom tooltip if it is the child of a <see cref="TooltipContainer"/>.
    /// Keep in mind that tooltips can only be displayed by a <see cref="TooltipContainer"/> if the <see cref="Drawable"/> implementing <see cref="IHasCustomTooltip"/> has <see cref="Drawable.HandlePositionalInput"/> set to true.
    /// </summary>
    public interface IHasCustomTooltip : ITooltipContentProvider
    {
        /// <summary>
        /// The custom tooltip that should be displayed.
        /// </summary>
        /// <remarks>
        /// A tooltip may be reused between different drawables with different content if they share the same tooltip type.
        /// Therefore it is recommended for all displayed content in the tooltip to be provided by <see cref="TooltipContent"/> instead.
        /// </remarks>
        /// <returns>The custom tooltip that should be displayed.</returns>
        ITooltip GetCustomTooltip();

        /// <summary>
        /// Tooltip text that shows when hovering the drawable.
        /// </summary>
        object? TooltipContent { get; }
    }

    /// <inheritdoc />
    public interface IHasCustomTooltip<TContent> : IHasCustomTooltip
    {
        ITooltip IHasCustomTooltip.GetCustomTooltip() => GetCustomTooltip();

        /// <inheritdoc cref="IHasCustomTooltip.GetCustomTooltip"/>
        new ITooltip<TContent> GetCustomTooltip();

        object? IHasCustomTooltip.TooltipContent => TooltipContent;

        /// <inheritdoc cref="IHasCustomTooltip.TooltipContent"/>
        new TContent? TooltipContent { get; }
    }
}
