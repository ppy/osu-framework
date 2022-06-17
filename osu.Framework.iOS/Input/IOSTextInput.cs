// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using Foundation;
using osu.Framework.Input;

namespace osu.Framework.iOS.Input
{
    public class IOSTextInput : TextInputSource
    {
        private readonly IOSGameHost host;
        private readonly IOSGameView view;

        public IOSTextInput(IOSGameHost host, IOSGameView view)
        {
            this.host = host;
            this.view = view;
        }

        private void handleShouldChangeCharacters(NSRange range, string text)
        {
            if (text == " " || text.Trim().Length > 0)
                TriggerTextInput(text);
        }

        protected override void ActivateTextInput(bool allowIme)
        {
            view.KeyboardTextField.HandleShouldChangeCharacters += handleShouldChangeCharacters;
            view.KeyboardTextField.UpdateFirstResponder(true);
            host.TextFieldHandler.KeyboardActive = true;
        }

        protected override void EnsureTextInputActivated(bool allowIme)
        {
            // If the user has manually closed the keyboard, it will not be shown until another TextBox is focused.
            // Calling `view.KeyboardTextField.UpdateFirstResponder` over and over again won't work, due to how
            // `responderSemaphore` currently works in that method.

            // TODO: add iOS implementation
        }

        protected override void DeactivateTextInput()
        {
            view.KeyboardTextField.HandleShouldChangeCharacters -= handleShouldChangeCharacters;
            view.KeyboardTextField.UpdateFirstResponder(false);
            host.TextFieldHandler.KeyboardActive = false;
        }
    }
}
