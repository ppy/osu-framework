// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

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

        private class ManualInputHandler : InputHandler
        {
            private Vector2 lastMousePosition;

            public void MoveMouseTo(Vector2 position)
            {
                PendingStates.Enqueue(new InputState { Mouse = new MouseState { Position = position } });
                lastMousePosition = position;
            }

            public void Click(MouseButton button)
            {
                var mouseState = new MouseState { Position = lastMousePosition };
                mouseState.SetPressed(button, true);

                PendingStates.Enqueue(new InputState { Mouse = mouseState });

                mouseState = (MouseState)mouseState.Clone();
                mouseState.SetPressed(button, false);

                PendingStates.Enqueue(new InputState { Mouse = mouseState });
            }

            public override bool Initialize(GameHost host) => true;
            public override bool IsActive => true;
            public override int Priority => 0;
        }
    }
}
