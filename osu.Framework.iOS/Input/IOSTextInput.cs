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

        public event Action<string> OnTextInput;

        public event Action<string> OnIMEComposition;

        public bool Active { get; private set; }

        public IOSTextInput(IOSGameView view)
        {
            this.view = view;
        }

        private void handleShouldChangeCharacters(NSRange range, string text)
        {
            if (Active && (text == " " || text.Trim().Length > 0))
                OnTextInput?.Invoke(text);
        }

        public void Activate()
        {
            view.KeyboardTextField.HandleShouldChangeCharacters += handleShouldChangeCharacters;
            view.KeyboardTextField.UpdateFirstResponder(true);
            Active = true;
        }

        public void Deactivate()
        {
            view.KeyboardTextField.HandleShouldChangeCharacters -= handleShouldChangeCharacters;
            view.KeyboardTextField.UpdateFirstResponder(false);
            Active = false;
        }

        public void EnsureActivated()
        {
            /// If the user has manually closed the keyboard, it will not be shown until another <see cref="Framework.Graphics.UserInterface.TextBox"/>
            /// is focused. Calling <see cref="IOSGameView.HiddenTextField.UpdateFirstResponder"/> over and over again won't work, due to how
            /// `responderSemaphore` currently works.

            // TODO: add iOS implementation
        }
    }
}
