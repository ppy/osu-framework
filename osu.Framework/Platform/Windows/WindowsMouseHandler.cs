// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Input.Handlers.Mouse;
using osuTK;

namespace osu.Framework.Platform.Windows
{
    public class WindowsMouseHandler : MouseHandler
    {
        protected override void HandleMouseMoveRelative(Vector2 delta)
        {
            // handled via WindowsRawInputMouseHandler.
        }
    }
}
