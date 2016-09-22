// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using osu.Framework.Lists;
using OpenTK;
using OpenTK.Input;

namespace osu.Framework.Input
{
    public class MouseState : IMouseState
    {
        public IMouseState LastState;

        public List<ButtonState> ButtonStates = new List<ButtonState>(new[]
        {
            new ButtonState(MouseButton.Left),
            new ButtonState(MouseButton.Middle),
            new ButtonState(MouseButton.Right),
            new ButtonState(MouseButton.Button1),
            new ButtonState(MouseButton.Button2)
        });

        public bool LeftButton => ButtonStates.Find(b => b.Button == MouseButton.Left).State;
        public bool RightButton => ButtonStates.Find(b => b.Button == MouseButton.Right).State;
        public bool MiddleButton => ButtonStates.Find(b => b.Button == MouseButton.Middle).State;
        public bool BackButton => ButtonStates.Find(b => b.Button == MouseButton.Button1).State;
        public bool ForwardButton => ButtonStates.Find(b => b.Button == MouseButton.Button2).State;

        public bool WheelUp;
        public bool WheelDown;

        public bool HasMainButtonPressed => LeftButton || RightButton;

        public Vector2 Delta => Position - (LastState?.Position ?? Vector2.Zero);

        public Vector2 Position { get; internal set; }

        public Vector2? PositionMouseDown { get; internal set; }

        public MouseState(IMouseState last = null)
        {
            LastState = last;
            PositionMouseDown = last?.PositionMouseDown;
        }

        public class ButtonState
        {
            public MouseButton Button;
            public bool State;

            public ButtonState(MouseButton button)
            {
                Button = button;
                State = false;
            }
        }
    }
}
