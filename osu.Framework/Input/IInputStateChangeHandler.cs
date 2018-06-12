// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK.Input;

namespace osu.Framework.Input
{
    /// <summary>
    /// A thing which can handle <see cref="InputState"/> changes.
    /// </summary>
    public interface IInputStateChangeHandler
    {
        /// <summary>
        /// Handles a change of mouse position.
        /// </summary>
        void HandleMousePositionChange(InputState state);

        /// <summary>
        /// Handles a change of mouse scroll.
        /// </summary>
        void HandleMouseScrollChange(InputState state);

        /// <summary>
        /// Handles a change of mouse button state.
        /// </summary>
        void HandleMouseButtonStateChange(InputState state, MouseButton button, ButtonStateChangeKind kind);

        /// <summary>
        /// Handles a change of keyboard key state.
        /// </summary>
        void HandleKeyboardKeyStateChange(InputState state, Key key, ButtonStateChangeKind kind);

        /// <summary>
        /// Handles a change of joystick button state.
        /// </summary>
        void HandleJoystickButtonStateChange(InputState state, JoystickButton button, ButtonStateChangeKind kind);

        /// <summary>
        /// Handles a change of things other than mouse, keyboard and joystick.
        /// </summary>
        /// <param name="state">Current state</param>
        /// <param name="input"><see cref="IInput"/> which made a change</param>
        void HandleCustomInput(InputState state, IInput input);
    }
}
