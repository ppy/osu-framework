// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

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
    }

    /// <inheritdoc />
    public interface ITooltip<in T> : ITooltip
    {
        void ITooltip.SetContent(object content) => SetContent((T)content);

        /// <inheritdoc cref="ITooltip.SetContent"/>
        void SetContent(T content);
    }
}
