// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osuTK;
using osuTK.Input;

namespace osu.Framework.Platform
{
    /// <summary>
    /// Aggregates the data returned from a MouseUp or MouseDown event.
    /// </summary>
    public struct MouseButtonInputArgs
    {
        /// <summary>
        /// The <see cref="MouseButton"/> that was either pressed or released.
        /// </summary>
        public MouseButton Button;

        /// <summary>
        /// True if <see cref="Button"/> was pressed, false if it was released.
        /// </summary>
        public bool Pressed;

        public MouseButtonInputArgs(MouseButton button, bool pressed)
        {
            Button = button;
            Pressed = pressed;
        }
    }

    /// <summary>
    /// Aggregates the data returned from a MouseMove event.
    /// </summary>
    public struct MouseMoveInputArgs
    {
        /// <summary>
        /// The new position for the mouse cursor.
        /// </summary>
        public Vector2 Position;

        public MouseMoveInputArgs(Vector2 position)
        {
            Position = position;
        }
    }

    /// <summary>
    /// Aggregates the data returned from a MouseWheel event.
    /// </summary>
    public struct MouseWheelInputArgs
    {
        /// <summary>
        /// The distance the mouse wheel has moved in both X and Y directions.
        /// </summary>
        public Vector2 Delta;

        /// <summary>
        /// True if triggered from a "precise" source, such as a trackpad gesture.
        /// </summary>
        public bool Precise;

        public MouseWheelInputArgs(Vector2 delta, bool precise)
        {
            Delta = delta;
            Precise = precise;
        }
    }

    /// <summary>
    /// Aggregates the data returned from a KeyUp or KeyDown event.
    /// </summary>
    public struct KeyPressInputArgs
    {
        /// <summary>
        /// The <see cref="osuTK.Input.Key"/> that was either pressed or released.
        /// </summary>
        public Key Key;

        /// <summary>
        /// True if <see cref="Key"/> was pressed, false if it was released.
        /// </summary>
        public bool Pressed;

        public KeyPressInputArgs(Key key, bool pressed)
        {
            Key = key;
            Pressed = pressed;
        }
    }

    /// <summary>
    /// Aggregates the data returned from a KeyTyped event.
    /// </summary>
    public struct KeyTypedInputArgs
    {
        /// <summary>
        /// The individual character that was typed.
        /// </summary>
        public char Character;

        public KeyTypedInputArgs(char character)
        {
            Character = character;
        }
    }
}
