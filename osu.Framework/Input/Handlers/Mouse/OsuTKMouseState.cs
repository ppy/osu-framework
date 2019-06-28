// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osuTK;
using osuTK.Input;

namespace osu.Framework.Input.Handlers.Mouse
{
    internal abstract class OsuTKMouseState : States.MouseState
    {
        public readonly bool WasActive;
        public readonly bool HasPreciseScroll;
        public MouseState RawState;

        protected OsuTKMouseState(MouseState tkState, bool active, Vector2? mappedPosition)
        {
            WasActive = active;

            RawState = tkState;

            // While not focused, let's silently ignore everything but position.
            if (active && tkState.IsAnyButtonDown)
            {
                addIfPressed(tkState.LeftButton, MouseButton.Left);
                addIfPressed(tkState.MiddleButton, MouseButton.Middle);
                addIfPressed(tkState.RightButton, MouseButton.Right);
                addIfPressed(tkState.XButton1, MouseButton.Button1);
                addIfPressed(tkState.XButton2, MouseButton.Button2);
            }

            Scroll = new Vector2(-tkState.Scroll.X, tkState.Scroll.Y);
            HasPreciseScroll = tkState.Flags.HasFlag(MouseStateFlags.HasPreciseScroll);
            Position = new Vector2(mappedPosition?.X ?? tkState.X, mappedPosition?.Y ?? tkState.Y);
        }

        private void addIfPressed(ButtonState tkState, MouseButton button)
        {
            if (tkState == ButtonState.Pressed)
                SetPressed(button, true);
        }
    }
}
