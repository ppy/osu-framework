// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Input.States;
using OpenTK.Input;

namespace osu.Framework.Input.StateChanges
{
    /// <summary>
    /// An object which can handle <see cref="InputState"/> changes.
    /// </summary>
    public interface IInputStateChangeHandler
    {
        /// <summary>
        /// Handles a change of mouse position.
        /// </summary>
        /// <param name="state">The current <see cref="InputState"/>.</param>
        void HandleMousePositionChange(InputState state);

        /// <summary>
        /// Handles a change of mouse scroll.
        /// </summary>
        /// <param name="state">The current <see cref="InputState"/>.</param>
        void HandleMouseScrollChange(InputState state);

        /// <summary>
        /// Handles a change of mouse button state.
        /// </summary>
        /// <param name="state">The current <see cref="InputState"/>.</param>
        /// <param name="button">The <see cref="MouseButton"/> that changed.</param>
        /// <param name="kind">The type of change that occurred on <paramref name="button"/>.</param>
        void HandleMouseButtonStateChange(InputState state, MouseButton button, ButtonStateChangeKind kind);

        /// <summary>
        /// Handles a change of keyboard key state.
        /// </summary>
        /// <param name="state">The current <see cref="InputState"/>.</param>
        /// <param name="key">The <see cref="Key"/> that changed.</param>
        /// <param name="kind">The type of change that occurred on <paramref name="key"/>.</param>
        void HandleKeyboardKeyStateChange(InputState state, Key key, ButtonStateChangeKind kind);

        /// <summary>
        /// Handles a change of joystick button state.
        /// </summary>
        /// <param name="state">The current <see cref="InputState"/>.</param>
        /// <param name="button">The <see cref="JoystickButton"/> that changed.</param>
        /// <param name="kind">The type of change that occurred on <paramref name="button"/>.</param>
        void HandleJoystickButtonStateChange(InputState state, JoystickButton button, ButtonStateChangeKind kind);

        /// <summary>
        /// Handles a change of things other than mouse, keyboard and joystick.
        /// </summary>
        /// <param name="state">The current state.</param>
        /// <param name="input"><see cref="IInput"/> which made a change.</param>
        void HandleCustomInput(InputState state, IInput input);
    }
}
