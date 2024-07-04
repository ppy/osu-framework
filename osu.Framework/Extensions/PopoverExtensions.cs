// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Cursor;

namespace osu.Framework.Extensions
{
    public static class PopoverExtensions
    {
        /// <summary>
        /// Shows the popover for <paramref name="hasPopover"/> on its nearest <see cref="PopoverContainer"/> ancestor.
        /// </summary>
        public static void ShowPopover(this IHasPopover hasPopover) => setTargetOnNearestPopover((Drawable)hasPopover, hasPopover);

        /// <summary>
        /// Hides the popover shown on <paramref name="drawable"/>'s nearest <see cref="PopoverContainer"/> ancestor.
        /// </summary>
        public static void HidePopover(this Drawable drawable) => setTargetOnNearestPopover(drawable, null);

        private static void setTargetOnNearestPopover(Drawable origin, IHasPopover? target)
        {
            var popoverContainer = origin as PopoverContainer
                                   ?? origin.FindClosestParent<PopoverContainer>()
                                   ?? throw new InvalidOperationException($"Cannot show or hide a popover without a parent {nameof(PopoverContainer)} in the hierarchy");

            popoverContainer.SetTarget(target);
        }
    }
}
