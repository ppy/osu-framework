// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osuTK;

namespace osu.Framework.Graphics.Cursor
{
    /// <summary>
    /// A tooltip that can be used in conjunction with a <see cref="TooltipContainer"/> and/or <see cref="IHasCustomTooltip"/> implementation.
    /// </summary>
    public interface ITooltip : IDrawable
    {
        /// <summary>
        /// Set new content be displayed on this tooltip.
        /// </summary>
        /// <param name="content">The content to be displayed.</param>
        void SetContent(object content);

        /// <summary>
        /// Moves the tooltip to the given position. May use easing.
        /// </summary>
        /// <param name="pos">The position the tooltip should be moved to.</param>
        void Move(Vector2 pos);

        /// <summary>
        /// Whether to allow the cursor to overlap the tooltip. If true, the tooltip will try to stay anchored
        /// to the bottom-right of the cursor while keeping itself on screen, potentially overlapping the cursor.
        /// If false, the tooltip will move to the top-right when the content doesn't fit with the current cursor location.
        /// </summary>
        /// <remarks>
        /// If true, this can be used to avoid abrupt position changes when the content is near the bottom window edge.
        /// </remarks>
        bool AllowCursorOverlap { get; }
    }

    /// <inheritdoc />
    public interface ITooltip<in T> : ITooltip
    {
        void ITooltip.SetContent(object content) => SetContent((T)content);

        /// <inheritdoc cref="ITooltip.SetContent"/>
        void SetContent(T content);
    }
}
