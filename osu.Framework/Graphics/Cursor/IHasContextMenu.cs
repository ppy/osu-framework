// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.UserInterface;

namespace osu.Framework.Graphics.Cursor
{
    public interface IHasContextMenu : IDrawable
    {
        /// <summary>
        /// Menu items that appear when the drawable is right-clicked.
        /// </summary>
        /// <remarks>
        /// If empty, this <see cref="Drawable"/> will be picked as the menu target but a context menu will not be shown.
        /// <para>If null, this <see cref="Drawable"/> will not be picked as the menu target and other <see cref="Drawable"/>s underneath may become the menu target.</para>
        /// </remarks>
        MenuItem[]? ContextMenuItems { get; }
    }
}
