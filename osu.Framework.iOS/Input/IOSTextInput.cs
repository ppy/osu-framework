// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Foundation;
using osu.Framework.Input;
using osu.Framework.Threading;

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

        private ScheduledDelegate resignDelegate;

        protected override void ActivateTextInput(bool allowIme)
        {
            resignDelegate?.Cancel();
            resignDelegate = null;

            view.KeyboardTextField.HandleShouldChangeCharacters += handleShouldChangeCharacters;
            view.KeyboardTextField.InvokeOnMainThread(() => view.KeyboardTextField.BecomeFirstResponder());
            host.TextFieldHandler.KeyboardActive = true;
        }

        protected override void EnsureTextInputActivated(bool allowIme)
        {
            resignDelegate?.Cancel();
            resignDelegate = null;

            view.KeyboardTextField.InvokeOnMainThread(() => view.KeyboardTextField.BecomeFirstResponder());
            host.TextFieldHandler.KeyboardActive = true;
        }

        protected override void DeactivateTextInput()
        {
            view.KeyboardTextField.HandleShouldChangeCharacters -= handleShouldChangeCharacters;
            host.TextFieldHandler.KeyboardActive = false;

            // text input may be deactivated and activated at the same frame, as a result of switching between textboxes.
            // this could cause the software keyboard to flicker, scheduling the operation to the next frame will help.
            // todo: that should probably be improved framework-side instead, but this should do for now.
            resignDelegate = host.InputThread.Scheduler.Add(() => view.KeyboardTextField.ResignFirstResponder());
        }
    }
}
