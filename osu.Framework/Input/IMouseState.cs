// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;

namespace osu.Framework.Input
{
    public interface IMouseState
    {
        bool BackButton { get; }
        bool ForwardButton { get; }

        IMouseState NativeState { get; }

        Vector2 Delta { get; }
        Vector2 Position { get; }

        Vector2 LastPosition { get; }

        Vector2? PositionMouseDown { get; }

        bool HasMainButtonPressed { get; }

        bool LeftButton { get; }
        bool MiddleButton { get; }
        bool RightButton { get; }

        int Wheel { get; }

        int WheelDelta { get; }
    }
}