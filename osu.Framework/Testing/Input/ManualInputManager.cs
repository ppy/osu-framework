// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics;
using osu.Framework.Input;
using osu.Framework.Input.Handlers;
using osu.Framework.Input.StateChanges;
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

        public void Input(IInput input)
        {
            UseParentInput = false;
            handler.EnqueueInput(input);
        }

        public void PressKey(Key key) => Input(new KeyboardKeyInput(key, true));
        public void ReleaseKey(Key key) => Input(new KeyboardKeyInput(key, false));

        public void ScrollBy(Vector2 delta, bool isPrecise = false) => Input(new MouseScrollRelativeInput { Delta = delta, IsPrecise = isPrecise });
        public void ScrollHorizontalBy(float delta, bool isPrecise = false) => ScrollBy(new Vector2(delta, 0), isPrecise);
        public void ScrollVerticalBy(float delta, bool isPrecise = false) => ScrollBy(new Vector2(0, delta), isPrecise);

        public void MoveMouseTo(Drawable drawable) => MoveMouseTo(drawable.ToScreenSpace(drawable.LayoutRectangle.Centre));
        public void MoveMouseTo(Vector2 position) => Input(new MousePositionAbsoluteInput { Position = position });

        public void Click(MouseButton button)
        {
            PressButton(button);
            ReleaseButton(button);
        }

        public void PressButton(MouseButton button) => Input(new MouseButtonInput(button, true));
        public void ReleaseButton(MouseButton button) => Input(new MouseButtonInput(button, false));

        public void PressJoystickButton(JoystickButton button) => Input(new JoystickButtonInput(button, true));
        public void ReleaseJoystickButton(JoystickButton button) => Input(new JoystickButtonInput(button, false));

        private class ManualInputHandler : InputHandler
        {
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
