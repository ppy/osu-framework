// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.UserInterface;

namespace osu.Framework.Graphics.Cursor
{
    public class BasicContextMenuContainer : ContextMenuContainer
    {
        protected override Menu CreateMenu() => new BasicMenu(Direction.Vertical);
    }
}
