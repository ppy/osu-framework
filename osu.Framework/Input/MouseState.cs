// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using OpenTK;
using OpenTK.Input;
using System.Linq;

namespace osu.Framework.Input
{
    public class MouseState : IMouseState
    {
        private const int mouse_button_count = (int)MouseButton.LastButton;

        public bool[] PressedButtons = new bool[mouse_button_count];

        public IMouseState NativeState => this;

        public IMouseState LastState { get; set; }

        public virtual int WheelDelta => Wheel - LastState?.Wheel ?? 0;

        public int Wheel { get; set; }

        public bool HasMainButtonPressed => IsPressed(MouseButton.Left) || IsPressed(MouseButton.Right);

        public bool HasAnyButtonPressed => PressedButtons.Any(b => b);

        public Vector2 Delta => Position - LastPosition;

        public Vector2 Position { get; protected set; }

        public Vector2 LastPosition => LastState?.Position ?? Position;

        public Vector2? PositionMouseDown { get; set; }

        public IMouseState Clone()
        {
            var clone = (MouseState)MemberwiseClone();

            clone.PressedButtons = new bool[mouse_button_count];
            Array.Copy(PressedButtons, clone.PressedButtons, mouse_button_count);

            clone.LastState = null;
            return clone;
        }

        public bool IsPressed(MouseButton button) => PressedButtons[(int)button];

        public void SetPressed(MouseButton button, bool pressed) => PressedButtons[(int)button] = pressed;
    }
}
