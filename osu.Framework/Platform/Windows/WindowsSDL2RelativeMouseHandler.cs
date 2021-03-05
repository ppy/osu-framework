// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Input.Handlers.Mouse;
using osuTK;

namespace osu.Framework.Platform.Windows
{
    public class WindowsSDL2RelativeMouseHandler : SDL2RelativeMouseHandler
    {
        protected override void HandleMouseMoveRelative(Vector2 delta)
        {
            // block.
        }
    }
}
