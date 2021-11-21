// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

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
            PendingText += text;
        }

        private void handleTextEditing(string text, int start, int length)
        {
            // TODO: add IME support
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
    }
}
