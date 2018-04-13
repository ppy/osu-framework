// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

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
