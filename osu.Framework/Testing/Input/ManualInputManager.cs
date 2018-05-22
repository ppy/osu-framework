// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;
using osu.Framework.Graphics;
using osu.Framework.Input;
using osu.Framework.Input.Handlers;
using osu.Framework.Platform;
using OpenTK;
using OpenTK.Input;
using MouseState = osu.Framework.Input.MouseState;

namespace osu.Framework.Testing.Input
{
    public class ManualInputManager : PassThroughInputManager
    {
        private readonly ManualInputHandler handler;

        public ManualInputManager()
        {
            UseParentState = true;
            AddHandler(handler = new ManualInputHandler());
        }

        public void PressKey(Key key)
        {
            UseParentState = false;
            handler.PressKey(key);
        }

        public void ReleaseKey(Key key)
        {
            UseParentState = false;
            handler.ReleaseKey(key);
        }

        public void ScrollBy(int delta)
        {
            UseParentState = false;
            handler.ScrollBy(delta);
        }

        public void MoveMouseTo(Drawable drawable)
        {
            UseParentState = false;
            MoveMouseTo(drawable.ToScreenSpace(drawable.LayoutRectangle.Centre));
        }

        public void MoveMouseTo(Vector2 position)
        {
            UseParentState = false;
            handler.MoveMouseTo(position);
        }

        public void Click(MouseButton button)
        {
            UseParentState = false;
            handler.Click(button);
        }

        public void ButtonDown(MouseButton button)
        {
            UseParentState = false;
            handler.ButtonDown(button);
        }

        public void ButtonUp(MouseButton button)
        {
            UseParentState = false;
            handler.ButtonUp(button);
        }

        private class ManualInputHandler : InputHandler
        {
            private readonly List<Key> pressedKeys = new List<Key>();
            private Vector2 lastMousePosition;
            private int lastWheel;

            public void PressKey(Key key)
            {
                pressedKeys.Add(key);
                PendingStates.Enqueue(new InputState { Keyboard = new Framework.Input.KeyboardState { Keys = pressedKeys } });
            }

            public void ReleaseKey(Key key)
            {
                if (!pressedKeys.Remove(key))
                    return;
                PendingStates.Enqueue(new InputState { Keyboard = new Framework.Input.KeyboardState { Keys = pressedKeys } });
            }

            public void ScrollBy(int delta)
            {
                PendingStates.Enqueue(new InputState
                {
                    Mouse = new MouseState
                    {
                        Position = lastMousePosition,
                        Wheel = lastWheel + delta
                    }
                });

                lastWheel += delta;
            }

            public void MoveMouseTo(Vector2 position)
            {
                PendingStates.Enqueue(new InputState
                {
                    Mouse = new MouseState
                    {
                        Position = position,
                        Wheel = lastWheel
                    }
                });

                lastMousePosition = position;
            }

            public void Click(MouseButton button)
            {
                ButtonDown(button);
                ButtonUp(button);
            }

            public void ButtonDown(MouseButton button)
            {
                var mouseState = new MouseState { Position = lastMousePosition, Wheel = lastWheel};
                mouseState.SetPressed(button, true);

                PendingStates.Enqueue(new InputState { Mouse = mouseState });
            }

            public void ButtonUp(MouseButton button)
            {
                var mouseState = new MouseState { Position = lastMousePosition, Wheel = lastWheel };
                mouseState.SetPressed(button, false);

                PendingStates.Enqueue(new InputState { Mouse = mouseState });
            }

            public override bool Initialize(GameHost host) => true;
            public override bool IsActive => true;
            public override int Priority => 0;
        }
    }
}
