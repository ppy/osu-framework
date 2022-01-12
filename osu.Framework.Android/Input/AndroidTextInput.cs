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
        private readonly AndroidGameActivity activity;
        private readonly InputMethodManager inputMethodManager;

        public AndroidTextInput(AndroidGameView view)
        {
            this.view = view;
            activity = (AndroidGameActivity)view.Context;

            if (view.Context != null)
                inputMethodManager = view.Context.GetSystemService(Context.InputMethodService) as InputMethodManager;
        }

        private void commitText(string text)
        {
            TriggerImeResult(text);
        }

        private void keyDown(Keycode arg, KeyEvent e)
        {
            if (e.UnicodeChar != 0)
                AddPendingText(((char)e.UnicodeChar).ToString());
        }

        protected override void ActivateTextInput(bool allowIme)
        {
            view.KeyDown += keyDown;
            view.CommitText += commitText;

            activity.RunOnUiThread(() =>
            {
                view.RequestFocus();
                inputMethodManager?.ShowSoftInput(view, 0);
            });
        }

        protected override void EnsureTextInputActivated(bool allowIme)
        {
            activity.RunOnUiThread(() =>
            {
                view.RequestFocus();
                inputMethodManager?.ShowSoftInput(view, 0);
            });
        }

        protected override void DeactivateTextInput()
        {
            view.KeyDown -= keyDown;
            view.CommitText -= commitText;

            activity.RunOnUiThread(() =>
            {
                inputMethodManager?.HideSoftInputFromWindow(view.WindowToken, HideSoftInputFlags.None);
                view.ClearFocus();
            });
        }
    }
}
