// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Android.Views;
using osu.Framework.Input;

namespace osu.Framework.Android.Input
{
    internal class AndroidTextInput : TextInputSource
    {
        private readonly AndroidGameView view;

        public AndroidTextInput(AndroidGameView view)
        {
            this.view = view;
        }

        private void commitText(string text)
        {
            TriggerTextInput(text);
        }

        private void keyDown(Keycode arg, KeyEvent e)
        {
            if (e.UnicodeChar != 0)
                TriggerTextInput(((char)e.UnicodeChar).ToString());
        }

        protected override void ActivateTextInput(bool allowIme)
        {
            view.KeyDown += keyDown;
            view.CommitText += commitText;
            view.StartTextInput();
        }

        protected override void EnsureTextInputActivated(bool allowIme)
        {
            view.StartTextInput();
        }

        protected override void DeactivateTextInput()
        {
            view.KeyDown -= keyDown;
            view.CommitText -= commitText;
            view.StopTextInput();
        }
    }
}
