// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Input.States;
using osu.Framework.Input.Handlers.Mouse;
using osu.Framework.Platform;

namespace osu.Framework.Input.StateChanges
{
    /// <summary>
    /// Denotes a change of <see cref="SDL2DesktopWindow.CursorInWindow"/>.
    /// Used only to propagate the change back to the <see cref="MouseHandler"/>.
    /// </summary>
    public class CursorInWindowChange : IInput
    {
        public void Apply(InputState state, IInputStateChangeHandler handler)
        {
            if (handler is UserInputManager userInputManager)
                userInputManager.HandleCursorInWindowChange();
        }
    }
}
