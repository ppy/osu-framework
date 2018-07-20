// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Graphics;
using osu.Framework.Input.States;
using OpenTK;
using OpenTK.Input;

namespace osu.Framework.Input
{
    /// <summary>
    /// Used for compatibility only. Will be removed after the new way is applied to code.
    /// This converts positions to a <see cref="Drawable"/>'s parent space choordinate.
    /// </summary>
    internal struct LocalMouseState : IMouseState
    {
        public IMouseState NativeState { get; private set; }

        private readonly Drawable us;

        public LocalMouseState(IMouseState state, Drawable us)
        {
            NativeState = state;
            this.us = us;
        }

        public Vector2 Delta => Position - LastPosition;

        public Vector2 Position
        {
            get => us.Parent?.ToLocalSpace(NativeState.Position) ?? NativeState.Position;
            set => throw new NotImplementedException();
        }

        public bool IsPositionValid
        {
            get => NativeState.IsPositionValid;
            set => throw new NotImplementedException();
        }

        public Vector2 LastPosition
        {
            get => us.Parent?.ToLocalSpace(NativeState.LastPosition) ?? NativeState.LastPosition;
            set => throw new NotImplementedException();
        }

        public Vector2? PositionMouseDown
        {
            get => NativeState.PositionMouseDown == null ? null : us.Parent?.ToLocalSpace(NativeState.PositionMouseDown.Value) ?? NativeState.PositionMouseDown;
            set => throw new NotImplementedException();
        }

        public Vector2 Scroll
        {
            get => NativeState.Scroll;
            set => throw new NotSupportedException();
        }

        public Vector2 LastScroll
        {
            get => NativeState.LastScroll;
            set => throw new NotSupportedException();
        }

        public Vector2 ScrollDelta => NativeState.ScrollDelta;

        public bool HasPreciseScroll
        {
            get => NativeState.HasPreciseScroll;
            set => NativeState.HasPreciseScroll = value;
        }

        public ButtonStates<MouseButton> Buttons => NativeState.Buttons;

        public bool HasMainButtonPressed => NativeState.HasMainButtonPressed;

        public bool HasAnyButtonPressed => NativeState.HasAnyButtonPressed;

        public IMouseState Clone()
        {
            var cloned = (LocalMouseState)MemberwiseClone();
            cloned.NativeState = NativeState.Clone();
            return cloned;
        }

        public bool IsPressed(MouseButton button)
        {
            return NativeState.IsPressed(button);
        }

        public void SetPressed(MouseButton button, bool pressed)
        {
            NativeState.SetPressed(button, pressed);
        }
    }
}
