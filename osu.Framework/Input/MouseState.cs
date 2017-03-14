// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;
using OpenTK;
using OpenTK.Input;

namespace osu.Framework.Input
{
    public class MouseState : IMouseState
    {
        public IMouseState LastState;

        public HashSet<MouseButton> PressedButtons = new HashSet<MouseButton>();

        public bool LeftButton => PressedButtons.Contains(MouseButton.Left);
        public bool RightButton => PressedButtons.Contains(MouseButton.Right);
        public bool MiddleButton => PressedButtons.Contains(MouseButton.Middle);
        public bool BackButton => PressedButtons.Contains(MouseButton.Button1);
        public bool ForwardButton => PressedButtons.Contains(MouseButton.Button2);

        public IMouseState NativeState => this;

        public virtual int WheelDelta => Wheel - (LastState?.Wheel ?? 0);

        public int Wheel { get; set; }

        public bool HasMainButtonPressed => LeftButton || RightButton;

        public bool HasAnyButtonPressed => PressedButtons.Count > 0;

        public Vector2 Delta => Position - (LastState?.Position ?? Vector2.Zero);

        public Vector2 Position { get; protected set; }

        public Vector2 LastPosition => LastState?.Position ?? Position;

        public Vector2? PositionMouseDown { get; internal set; }

        public void SetLast(IMouseState last)
        {
            (last as MouseState)?.SetLast(null);

            LastState = last;
            if (last != null)
                PositionMouseDown = last.PositionMouseDown;
        }

        public MouseState Clone()
        {
            var clone = (MouseState)MemberwiseClone();
            clone.PressedButtons = new HashSet<MouseButton>(PressedButtons);
            clone.LastState = null;
            return clone;
        }

        public bool IsPressed(MouseButton button) => PressedButtons.Contains(button);
    }
}
