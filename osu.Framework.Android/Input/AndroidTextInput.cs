// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Android.Content;
using Android.Views;
using Android.Views.InputMethods;
using osu.Framework.Input;

namespace osu.Framework.Android.Input
{
    public class AndroidTextInput : TextInputSource
    {
        private readonly AndroidGameView view;
        private readonly InputMethodManager inputMethodManager;

        public AndroidTextInput(AndroidGameView view)
        {
            this.view = view;
            inputMethodManager = view.Activity.GetSystemService(Context.InputMethodService) as InputMethodManager;
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

            view.Activity.RunOnUiThread(() =>
            {
                view.RequestFocus();
                inputMethodManager?.ShowSoftInput(view, 0);
            });
        }

        protected override void EnsureTextInputActivated(bool allowIme)
        {
            view.Activity.RunOnUiThread(() =>
            {
                view.RequestFocus();
                inputMethodManager?.ShowSoftInput(view, 0);
            });
        }

        protected override void DeactivateTextInput()
        {
            view.KeyDown -= keyDown;
            view.CommitText -= commitText;

            view.Activity.RunOnUiThread(() =>
            {
                inputMethodManager?.HideSoftInputFromWindow(view.WindowToken, HideSoftInputFlags.None);
                view.ClearFocus();
            });
        }
    }
}
