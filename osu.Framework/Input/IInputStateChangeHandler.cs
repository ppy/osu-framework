// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK.Input;

namespace osu.Framework.Input
{
    public interface IInputStateChangeHandler
    {
        void HandleMousePositionChange(InputState state);
        void HandleMouseScrollChange(InputState state);
        void HandleMouseButtonStateChange(InputState state, MouseButton button, ButtonStateChangeKind kind);
        void HandleKeyboardKeyStateChange(InputState state, Key key, ButtonStateChangeKind kind);
        void HandleJoystickAxisChange(InputState state, JoystickAxis axis, int axisValue);
        void HandleJoystickButtonStateChange(InputState state, JoystickButton button, ButtonStateChangeKind kind);
    }
}
