// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;
using OpenTK;
using OpenTK.Input;
using System.Linq;
using osu.Framework.Extensions.TypeExtensions;

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

        public virtual int WheelDelta => Wheel - LastWheel;

        public int Wheel { get; set; }

        private int? lastWheel;

        public int LastWheel
        {
            get => lastWheel ?? Wheel;
            set => lastWheel = value;
        }

        public bool HasMainButtonPressed => IsPressed(MouseButton.Left) || IsPressed(MouseButton.Right);

        public bool HasAnyButtonPressed => buttons.Any();

        public Vector2 Delta => Position - LastPosition;

        public Vector2 Position { get; set; }

        private Vector2? lastPosition;

        public Vector2 LastPosition
        {
            get => lastPosition ?? Position;
            set => lastPosition = value;
        }

        public Vector2? PositionMouseDown { get; set; }

        public IMouseState Clone()
        {
            var clone = (MouseState)MemberwiseClone();
            clone.buttons = new List<MouseButton>(buttons);
            return clone;
        }

        public MouseState CloneWithoutDeltas()
        {
            var clone = (MouseState)Clone();
            clone.lastWheel = null;
            clone.lastPosition = null;
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

        public override string ToString()
        {
            string down = PositionMouseDown != null ? $"(down @ {PositionMouseDown.Value.X:#,0},{PositionMouseDown.Value.Y:#,0})" : string.Empty;
            return $@"{GetType().ReadableName()} ({Position.X:#,0},{Position.Y:#,0}) {down} {string.Join(",", Buttons.Select(b => b.ToString()))} Wheel {Wheel}/{WheelDelta}";
        }
    }
}
