// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Extensions.TypeExtensions;
using OpenTK;
using OpenTK.Input;

namespace osu.Framework.Input.States
{
    public class MouseState : IMouseState
    {
        public ButtonStates<MouseButton> Buttons { get; private set; } = new ButtonStates<MouseButton>();

        public Vector2 Scroll { get; set; }

        public bool HasMainButtonPressed => Buttons.IsPressed(MouseButton.Left) || Buttons.IsPressed(MouseButton.Right);

        public bool HasAnyButtonPressed => Buttons.HasAnyButtonPressed;

        public Vector2 Position { get; set; }

        public bool IsPositionValid { get; set; } = true;

        public IMouseState Clone()
        {
            var clone = (MouseState)MemberwiseClone();
            clone.Buttons = Buttons.Clone();
            return clone;
        }

        public bool IsPressed(MouseButton button) => Buttons.IsPressed(button);
        public void SetPressed(MouseButton button, bool pressed) => Buttons.SetPressed(button, pressed);

        public override string ToString()
        {
            string position = IsPositionValid ? $"({ Position.X:F0},{ Position.Y:F0})" : "(Invalid)";
            return $@"{GetType().ReadableName()} {position} {Buttons} Scroll ({Scroll.X:F2},{Scroll.Y:F2})";
        }
    }
}
