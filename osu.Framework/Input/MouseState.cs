// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;
using OpenTK;
using OpenTK.Input;
using System.Linq;

namespace osu.Framework.Input
{
    public class MouseState : IMouseState
    {
        public IReadOnlyList<MouseButton> Buttons
        {
            get { return buttons; }
            set
            {
                buttons.Clear();
                buttons.AddRange(value);
            }
        }

        private List<MouseButton> buttons { get; set; } = new List<MouseButton>();

        public IMouseState NativeState => this;

        public IMouseState LastState { get; set; }

        public virtual int WheelDelta => Wheel - LastState?.Wheel ?? 0;

        public int Wheel { get; set; }

        public bool HasMainButtonPressed => IsPressed(MouseButton.Left) || IsPressed(MouseButton.Right);

        public bool HasAnyButtonPressed => buttons.Any();

        public Vector2 Delta => Position - LastPosition;

        public Vector2 Position { get; set; }

        public Vector2 LastPosition => LastState?.Position ?? Position;

        public Vector2? PositionMouseDown { get; set; }

        public IMouseState Clone()
        {
            var clone = (MouseState)MemberwiseClone();
            clone.buttons = new List<MouseButton>(buttons);
            clone.LastState = null;
            return clone;
        }

        public bool IsPressed(MouseButton button) => buttons.Contains(button);

        public void SetPressed(MouseButton button, bool pressed)
        {
            if (buttons.Contains(button) == pressed)
                return;

            if (pressed)
                buttons.Add(button);
            else
                buttons.Remove(button);
        }
    }
}
