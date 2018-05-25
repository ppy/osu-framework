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

        public void PressButton(MouseButton button)
        {
            UseParentState = false;
            handler.PressButton(button);
        }

        public void ReleaseButton(MouseButton button)
        {
            UseParentState = false;
            handler.ReleaseButton(button);
        }

        public void AddStates(params InputState[] states)
        {
            UseParentState = false;
            foreach (var state in states)
                handler.EnqueueState(state);
        }

        private class ManualInputHandler : InputHandler
        {
            private readonly List<Key> pressedKeys = new List<Key>();

            private MouseState lastMouseState = new MouseState();

            public void PressKey(Key key)
            {
                pressedKeys.Add(key);
                EnqueueState(new InputState { Keyboard = new Framework.Input.KeyboardState { Keys = pressedKeys } });
            }

            public void ReleaseKey(Key key)
            {
                if (!pressedKeys.Remove(key))
                    return;
                EnqueueState(new InputState { Keyboard = new Framework.Input.KeyboardState { Keys = pressedKeys } });
            }

            public void PressButton(MouseButton button)
            {
                var state = lastMouseState.Clone();
                state.SetPressed(button, true);
                EnqueueState(new InputState { Mouse = state });
            }

            public void ReleaseButton(MouseButton button)
            {
                var state = lastMouseState.Clone();
                state.SetPressed(button, false);
                EnqueueState(new InputState { Mouse = state });
            }

            public void ScrollBy(int delta)
            {
                var state = lastMouseState.Clone();
                state.Wheel += delta;
                EnqueueState(new InputState { Mouse = state });
            }

            public void MoveMouseTo(Vector2 position)
            {
                var state = lastMouseState.Clone();
                state.Position = position;
                EnqueueState(new InputState { Mouse = state });
            }

            public void Click(MouseButton button)
            {
                PressButton(button);
                ReleaseButton(button);
            }

            public override bool Initialize(GameHost host) => true;
            public override bool IsActive => true;
            public override int Priority => 0;

            public void EnqueueState(InputState state)
            {
                if (state.Mouse is MouseState ms)
                    lastMouseState = ms;

                PendingStates.Enqueue(state);
            }
        }
    }
}
