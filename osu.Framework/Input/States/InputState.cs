// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

namespace osu.Framework.Input.States
{
    /// <summary>
    /// An object that stores all input states.
    /// </summary>
    public class InputState
    {
        /// <summary>
        /// The mouse state.
        /// </summary>
        public readonly MouseState Mouse;

        /// <summary>
        /// The keyboard state.
        /// </summary>
        public readonly KeyboardState Keyboard;

        /// <summary>
        /// The touch state.
        /// </summary>
        public readonly TouchState Touch;

        /// <summary>
        /// The joystick state.
        /// </summary>
        public readonly JoystickState Joystick;

        /// <summary>
        /// The midi state.
        /// </summary>
        public readonly MidiState Midi;

        /// <summary>
        /// The tablet state.
        /// </summary>
        public readonly TabletState Tablet;

        /// <summary>
        /// Creates a new <see cref="InputState"/> using the individual input states from another <see cref="InputState"/>.
        /// </summary>
        /// <param name="other">The <see cref="InputState"/> to take the individual input states from. Note that states are not cloned and will remain as references to the same objects.</param>
        public InputState(InputState other)
            : this(other.Mouse, other.Keyboard, other.Touch, other.Joystick, other.Midi, other.Tablet)
        {
        }

        /// <summary>
        /// Creates a new <see cref="InputState"/> using given individual input states.
        /// </summary>
        /// <param name="mouse">The mouse state.</param>
        /// <param name="keyboard">The keyboard state.</param>
        /// <param name="touch">The touch state.</param>
        /// <param name="joystick">The joystick state.</param>
        /// <param name="midi">The midi state.</param>
        /// <param name="tablet">The tablet state.</param>
        public InputState(MouseState mouse = null, KeyboardState keyboard = null, TouchState touch = null, JoystickState joystick = null, MidiState midi = null, TabletState tablet = null)
        {
            Mouse = mouse ?? new MouseState();
            Keyboard = keyboard ?? new KeyboardState();
            Touch = touch ?? new TouchState();
            Joystick = joystick ?? new JoystickState();
            Midi = midi ?? new MidiState();
            Tablet = tablet ?? new TabletState();
        }
    }
}
