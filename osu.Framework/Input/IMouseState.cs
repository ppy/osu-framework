// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
using OpenTK.Input;

namespace osu.Framework.Input
{
    public interface IMouseState
    {
        IMouseState NativeState { get; }

        ButtonStates<MouseButton> Buttons { get; }

        Vector2 Delta { get; }

        Vector2 Position { get; set; }

        Vector2 LastPosition { get; set; }

        Vector2? PositionMouseDown { get; set; }

        bool HasMainButtonPressed { get; }

        bool HasAnyButtonPressed { get; }

        bool IsPressed(MouseButton button);

        void SetPressed(MouseButton button, bool pressed);

        Vector2 Scroll { get; set; }

        Vector2 LastScroll { get; set; }

        Vector2 ScrollDelta { get; }

        bool HasPreciseScroll { get; set; }

        IMouseState Clone();
    }
}
