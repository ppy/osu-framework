// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Foundation;
using osu.Framework.Input;

namespace osu.Framework.iOS.Input
{
    public class IOSTextInput : ITextInputSource
    {
        private readonly IOSGameView view;

        private string pending = string.Empty;

        public IOSTextInput(IOSGameView view)
        {
            this.view = view;
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

        private void handleShouldChangeCharacters(NSRange range, string text)
        {
            if (text == " " || text.Trim().Length > 0)
                pending += text;
        }

        public void Activate()
        {
            view.KeyboardTextField.HandleShouldChangeCharacters += handleShouldChangeCharacters;
            view.KeyboardTextField.UpdateFirstResponder(true);
        }

        public void EnsureActivated()
        {
            // If the user has manually closed the keyboard, it will not be shown until another TextBox is focused.
            // Calling `view.KeyboardTextField.UpdateFirstResponder` over and over again won't work, due to how
            // `responderSemaphore` currently works in that method.

            // TODO: add iOS implementation
        }

        public void Deactivate()
        {
            view.KeyboardTextField.HandleShouldChangeCharacters -= handleShouldChangeCharacters;
            view.KeyboardTextField.UpdateFirstResponder(false);
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
