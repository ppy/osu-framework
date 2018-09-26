// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using osu.Framework.Extensions.TypeExtensions;
using osu.Framework.Graphics;
using osu.Framework.Input.States;
using OpenTK;
using OpenTK.Input;

namespace osu.Framework.Input.Events
{
    /// <summary>
    /// Denotes a UI event.
    /// A UI event is produced for and can be handled by a <see cref="Drawable"/>.
    /// While handling events, the <see cref="Target"/> is set to the <see cref="Drawable"/> which is handling the event.
    /// </summary>
    public abstract class UIEvent
    {
        /// <summary>
        /// The current input state.
        /// </summary>
        /// <remarks>
        /// This raw state should not be used for event handling if not really needed.
        /// Instead, properties such as <see cref="MousePosition"/> and event data should be used.
        /// </remarks>
        [NotNull]
        public readonly InputState CurrentState;

        /// <summary>
        /// The current target <see cref="Drawable"/> of this event.
        /// This can be modified to reuse an instance many times.
        /// </summary>
        [CanBeNull]
        public Drawable Target;

        /// <summary>
        /// Convert a coordinate to <see cref="Target"/>'s parent space.
        /// </summary>
        protected Vector2 ToLocalSpace(Vector2 screenSpacePosition) => Target?.Parent?.ToLocalSpace(screenSpacePosition) ?? screenSpacePosition;

        /// <summary>
        /// The current mouse position in screen space.
        /// </summary>
        public Vector2 ScreenSpaceMousePosition => CurrentState.Mouse.Position;

        /// <summary>
        /// The current mouse position in <see cref="Target"/>'s parent space.
        /// </summary>
        public Vector2 MousePosition => ToLocalSpace(ScreenSpaceMousePosition);

        /// <summary>
        /// Whether a specific mouse button is pressed.
        /// </summary>
        public bool IsPressed(MouseButton button) => CurrentState.Mouse.Buttons.IsPressed(button);

        /// <summary>
        /// Whether any mouse button is pressed.
        /// </summary>
        public bool HasAnyButtonPressed => CurrentState.Mouse.Buttons.HasAnyButtonPressed;

        /// <summary>
        /// List of currently pressed mouse buttons.
        /// </summary>
        public IEnumerable<MouseButton> PressedButtons => CurrentState.Mouse.Buttons;

        /// <summary>
        /// Whether a specific key is pressed.
        /// </summary>
        public bool IsPressed(Key key) => CurrentState.Keyboard.Keys.IsPressed(key);

        /// <summary>
        /// Whether any key is pressed.
        /// </summary>
        public bool HasAnyKeyPressed => CurrentState.Keyboard.Keys.HasAnyButtonPressed;

        /// <summary>
        /// Whether left or right control key is pressed.
        /// </summary>
        public bool ControlPressed => IsPressed(Key.LControl) || IsPressed(Key.RControl);

        /// <summary>
        /// Whether left or right alt key is pressed.
        /// </summary>
        public bool AltPressed => IsPressed(Key.LAlt) || IsPressed(Key.RAlt);

        /// <summary>
        /// Whether left or right shift key is pressed.
        /// </summary>
        public bool ShiftPressed => IsPressed(Key.LShift) || IsPressed(Key.RShift);

        /// <summary>
        /// Whether (Win key on Windows, or Command key on Mac) is pressed.
        /// </summary>
        public bool SuperPressed => IsPressed(Key.LWin) || IsPressed(Key.RWin);

        /// <summary>
        /// List of currently pressed keys.
        /// </summary>
        public IEnumerable<Key> PressedKeys => CurrentState.Keyboard.Keys;

        /// <summary>
        /// List of currently pressed joystick buttons.
        /// </summary>
        public IEnumerable<JoystickButton> PressedJoystickButtons => CurrentState.Joystick.Buttons;

        /// <summary>
        /// List of joystick axes. Axes which have zero value may be omitted.
        /// </summary>
        public IEnumerable<JoystickAxis> JoystickAxes => CurrentState.Joystick.Axes;

        protected UIEvent([NotNull] InputState state)
        {
            CurrentState = state ?? throw new ArgumentNullException(nameof(state));
        }

        public override string ToString() => $"{GetType().ReadableName()}()";
    }
}
