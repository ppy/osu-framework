// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Platform;

namespace osu.Framework.Input
{
    public class SDL2DesktopWindowTextInput : ITextInputSource
    {
        private readonly SDL2DesktopWindow window;
        private string pending = string.Empty;

        public SDL2DesktopWindowTextInput(SDL2DesktopWindow window)
        {
            this.window = window;
        }

        public bool ImeActive => false;

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
            pending += text;
        }

        private void handleTextEditing(string text, int start, int length)
        {
            // TODO: add IME support
        }

        public void Activate()
        {
            window.TextInput += handleTextInput;
            window.TextEditing += handleTextEditing;
            window.StartTextInput();
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
        }

        public event Action<string> OnNewImeComposition
        {
            add { }
            remove { }
        }

        public event Action<string> OnNewImeResult
        {
            add { }
            remove { }
        }
    }
}
