// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK.Input;

namespace osu.Framework.Input
{
    public interface IInputStateChangeHandler
    {
        void HandleMousePositionChange(InputState state);
        void HandleMouseScrollChange(InputState state);
        void HandleMouseButtonChange(InputState state, MouseButton button, ButtonStateChangeKind kind);
        void HandleKeyboardChange(InputState state, Key key, ButtonStateChangeKind kind);
        void HandleJoystickAxisChange(InputState state, JoystickAxis axis, int axisValue);
        void HandleJoystickButtonChange(InputState state, JoystickButton button, ButtonStateChangeKind kind);
    }
}
