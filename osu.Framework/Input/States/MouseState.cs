// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Extensions.TypeExtensions;
using osu.Framework.Input.StateChanges;
using osuTK;
using osuTK.Input;

namespace osu.Framework.Input.States
{
    public class MouseState
    {
        public readonly ButtonStates<MouseButton> Buttons = new ButtonStates<MouseButton>();

        public Vector2 Scroll { get; set; }

        public Vector2 Position { get; set; }

        public bool IsPositionValid { get; set; } = true;

        /// <summary>
        /// The last input source to make a change to the state.
        /// </summary>
        public IInput LastSource { get; set; }

        public bool IsPressed(MouseButton button) => Buttons.IsPressed(button);
        public void SetPressed(MouseButton button, bool pressed) => Buttons.SetPressed(button, pressed);

        public override string ToString()
        {
            string position = IsPositionValid ? $"({Position.X:F0},{Position.Y:F0})" : "(Invalid)";
            return $@"{GetType().ReadableName()} {position} {Buttons} Scroll ({Scroll.X:F2},{Scroll.Y:F2})";
        }
    }
}
