// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Primitives;
using osu.Framework.Platform;

namespace osu.Framework.Input
{
    public class SDL2DesktopWindowTextInput : TextInputSource
    {
        private readonly SDL2DesktopWindow window;

        public SDL2DesktopWindowTextInput(SDL2DesktopWindow window)
        {
            this.window = window;
        }

        private void handleTextInput(string text)
        {
            if (ImeActive)
            {
                // SDL sends IME results as `SDL_TextInputEvent` which we can't differentiate from regular text input
                // so we have to manually keep track and invoke the correct event.
                TriggerImeResult(text);
                ImeActive = false;
            }
            else
            {
                AddPendingText(text);
            }
        }

        private void handleTextEditing(string text, int selectionStart, int selectionLength)
        {
            if (text == null) return;

            // SDL sends empty text on composition end
            // https://github.com/libsdl-org/SDL/blob/1fc25bd83902df65e666f0cf0aa4dc717ade0748/src/video/windows/SDL_windowskeyboard.c#L934-L939
            ImeActive = !string.IsNullOrEmpty(text);

            TriggerImeComposition(text, selectionStart, selectionLength);
        }

        protected override void ActivateTextInput()
        {
            window.TextInput += handleTextInput;
            window.TextEditing += handleTextEditing;
            window.StartTextInput();
        }

        protected override void EnsureTextInputActivated()
        {
            window.StartTextInput();
        }

        protected override void DeactivateTextInput()
        {
            window.TextInput -= handleTextInput;
            window.TextEditing -= handleTextEditing;
            window.StopTextInput();
        }

        public override void SetImeRectangle(RectangleF rectangle)
        {
            window.SetTextInputRect(rectangle);
        }

        public override void ResetIme()
        {
            ImeActive = false;
            window.StopTextInput();

            EnsureActivated();
        }
    }
}
