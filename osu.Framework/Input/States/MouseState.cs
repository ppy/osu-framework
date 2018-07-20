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

        public IMouseState NativeState => this;

        public virtual Vector2 ScrollDelta => Scroll - LastScroll;

        public Vector2 Scroll { get; set; }

        private Vector2? lastScroll;

        public Vector2 LastScroll
        {
            get => lastScroll ?? new Vector2();
            set => lastScroll = value;
        }

        public bool HasPreciseScroll { get; set; }

        public bool HasMainButtonPressed => Buttons.IsPressed(MouseButton.Left) || Buttons.IsPressed(MouseButton.Right);

        public bool HasAnyButtonPressed => Buttons.HasAnyButtonPressed;

        public Vector2 Delta => Position - LastPosition;

        public Vector2 Position { get; set; }

        public bool IsPositionValid { get; set; } = true;

        private Vector2? lastPosition;

        public Vector2 LastPosition
        {
            get => lastPosition ?? Position;
            set => lastPosition = value;
        }

        public bool HasLastPosition => lastPosition.HasValue;

        public Vector2? PositionMouseDown { get; set; }

        public IMouseState Clone()
        {
            var clone = (MouseState)MemberwiseClone();
            clone.Buttons = Buttons.Clone();
            return clone;
        }

        public MouseState CloneWithoutDeltas()
        {
            var clone = (MouseState)Clone();
            clone.lastScroll = null;
            clone.lastPosition = null;
            return clone;
        }

        public bool IsPressed(MouseButton button) => Buttons.IsPressed(button);
        public void SetPressed(MouseButton button, bool pressed) => Buttons.SetPressed(button, pressed);

        public override string ToString()
        {
            string position = IsPositionValid ? $"({ Position.X:F0},{ Position.Y:F0})" : "(Invalid)";
            string down = PositionMouseDown != null ? $"(down @ {PositionMouseDown.Value.X:F0},{PositionMouseDown.Value.Y:F0})" : string.Empty;
            return $@"{GetType().ReadableName()} {position} {down} {Buttons} Scroll ({Scroll.X:F2},{Scroll.Y:F2})/({ScrollDelta.X:F2},{ScrollDelta.Y:F2})";
        }
    }
}
