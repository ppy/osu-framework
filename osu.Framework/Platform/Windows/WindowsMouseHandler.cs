// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Input.Handlers.Mouse;
using osuTK;

namespace osu.Framework.Platform.Windows
{
    public class WindowsMouseHandler : MouseHandler
    {
        private readonly Func<bool> isRawInputActive;

        public WindowsMouseHandler(Func<bool> isRawInputActive)
        {
            this.isRawInputActive = isRawInputActive;
        }

        protected override void HandleMouseMoveRelative(Vector2 delta)
        {
            if (isRawInputActive())
            {
                // handled via WindowsRawInputMouseHandler.
                return;
            }

            base.HandleMouseMoveRelative(delta);
        }
    }
}
