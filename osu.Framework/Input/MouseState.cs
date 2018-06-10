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
            get => buttons;
            set
            {
                buttons.Clear();
                buttons.AddRange(value);
            }
        }

        private List<MouseButton> buttons { get; set; } = new List<MouseButton>();

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

        public bool HasLastPosition => lastPosition.HasValue;

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
            clone.lastScroll = null;
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
            string down = PositionMouseDown != null ? $"(down @ {PositionMouseDown.Value.X:F0},{PositionMouseDown.Value.Y:F0})" : string.Empty;
            return $@"{GetType().ReadableName()} ({Position.X:F0},{Position.Y:F0}) {down} {string.Join(",", Buttons.Select(b => b.ToString()))} Scroll ({Scroll.X:F2},{Scroll.Y:F2})/({ScrollDelta.X:F2},{ScrollDelta.Y:F2})";
        }
    }
}
