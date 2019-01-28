﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
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
        /// The text to display on the tooltip.
        /// </summary>
        string TooltipText { set; }

        /// <summary>
        /// Refreshes the tooltip, updating potential non-text elements such as textures and colours.
        /// </summary>
        void Refresh();

        /// <summary>
        /// Moves the tooltip to the given position. May use easing.
        /// </summary>
        /// <param name="pos">The position the tooltip should be moved to.</param>
        void Move(Vector2 pos);
    }
}
