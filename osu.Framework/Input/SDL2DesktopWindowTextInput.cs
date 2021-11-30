// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Platform;

namespace osu.Framework.Input
{
    public class SDL2DesktopWindowTextInput : ITextInputSource
    {
        private readonly SDL2DesktopWindow window;
        private string pending = string.Empty;

        /// <summary>
        /// Whether one or more consumers requested this <see cref="ITextInputSource"/> to be active.
        /// </summary>
        private bool active;

        public SDL2DesktopWindowTextInput(SDL2DesktopWindow window)
        {
            this.window = window;
        }

        public bool ImeActive { get; private set; }

        public string GetPendingText()
        {
            try
            {
                return pending;
            }
            finally
            {
                pending = string.Empty;
            }
        }

        private void handleTextInput(string text)
        {
            if (ImeActive)
            {
                // SDL sends IME results as `SDL_TextInputEvent` which we can't differentiate from regular text input
                // so we have to manually keep track and invoke the correct event.
                OnNewImeResult?.Invoke(text);
                ImeActive = false;
            }
            else
            {
                pending += text;
            }
        }

        private void handleTextEditing(string text, int selectionStart, int selectionLength)
        {
            if (text == null) return;

            // SDL sends empty text on composition end
            // https://github.com/libsdl-org/SDL/blob/1fc25bd83902df65e666f0cf0aa4dc717ade0748/src/video/windows/SDL_windowskeyboard.c#L934-L939
            ImeActive = !string.IsNullOrEmpty(text);

            OnNewImeComposition?.Invoke(text, selectionStart, selectionLength);
        }

        public void Activate()
        {
            window.TextInput += handleTextInput;
            window.TextEditing += handleTextEditing;
            window.StartTextInput();
            active = true;
        }

        public void EnsureActivated()
        {
            window.StartTextInput();
        }

        public void Deactivate()
        {
            window.TextInput -= handleTextInput;
            window.TextEditing -= handleTextEditing;
            window.StopTextInput();
            active = false;
        }

        public void SetImeRectangle(RectangleF rectangle)
        {
            window.SetTextInputRect(rectangle);
        }

        public void ResetIme()
        {
            ImeActive = false;
            window.StopTextInput();

            if (active) EnsureActivated();
        }

        public event ITextInputSource.ImeCompositionDelegate OnNewImeComposition;
        public event Action<string> OnNewImeResult;
    }
}
