// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics;
using osu.Framework.Input;
using osu.Framework.Input.Handlers;
using osu.Framework.Platform;
using OpenTK;
using OpenTK.Input;

namespace osu.Framework.Testing.Input
{
    public class ManualInputManager : PassThroughInputManager
    {
        private readonly ManualInputHandler handler;

        public ManualInputManager()
        {
            UseParentInput = true;
            AddHandler(handler = new ManualInputHandler());
        }

        public void PressKey(Key key)
        {
            UseParentInput = false;
            handler.PressKey(key);
        }

        public void ReleaseKey(Key key)
        {
            UseParentInput = false;
            handler.ReleaseKey(key);
        }

        public void ScrollBy(Vector2 delta)
        {
            UseParentInput = false;
            handler.ScrollBy(delta);
        }

        public void ScrollHorizontalBy(float delta) => ScrollBy(new Vector2(delta, 0));

        public void ScrollVerticalBy(float delta) => ScrollBy(new Vector2(0, delta));

        public void MoveMouseTo(Drawable drawable)
        {
            UseParentInput = false;
            MoveMouseTo(drawable.ToScreenSpace(drawable.LayoutRectangle.Centre));
        }

        public void MoveMouseTo(Vector2 position)
        {
            UseParentInput = false;
            handler.MoveMouseTo(position);
        }

        public void Click(MouseButton button)
        {
            UseParentInput = false;
            handler.Click(button);
        }

        public void PressButton(MouseButton button)
        {
            UseParentInput = false;
            handler.PressButton(button);
        }

        public void ReleaseButton(MouseButton button)
        {
            UseParentInput = false;
            handler.ReleaseButton(button);
        }

        private class ManualInputHandler : InputHandler
        {
            public void PressKey(Key key)
            {
                PendingInputs.Enqueue(new KeyboardKeyInput(key, true));
            }

            public void ReleaseKey(Key key)
            {
                PendingInputs.Enqueue(new KeyboardKeyInput(key, false));
            }

            public void PressButton(MouseButton button)
            {
                PendingInputs.Enqueue(new MouseButtonInput(button, true));
            }

            public void ReleaseButton(MouseButton button)
            {
                PendingInputs.Enqueue(new MouseButtonInput(button, false));
            }

            public void ScrollBy(Vector2 delta)
            {
                PendingInputs.Enqueue(new MouseScrollRelativeInput { Delta = delta, IsPrecise = false });
            }

            public void ScrollVerticalBy(float delta) => ScrollBy(new Vector2(0, delta));

            public void ScrollHorizontalBy(float delta) => ScrollBy(new Vector2(delta, 0));

            public void MoveMouseTo(Vector2 position)
            {
                PendingInputs.Enqueue(new MousePositionAbsoluteInput { Position = position });
            }

            public void Click(MouseButton button)
            {
                PressButton(button);
                ReleaseButton(button);
            }

            public override bool Initialize(GameHost host) => true;
            public override bool IsActive => true;
            public override int Priority => 0;

            public void EnqueueInput(IInput input)
            {
                PendingInputs.Enqueue(input);
            }
        }
    }
}
