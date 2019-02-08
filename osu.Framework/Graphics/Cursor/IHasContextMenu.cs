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
        MenuItem[] ContextMenuItems { get; }
    }
}
