// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Android.Content;
using Android.Views;
using Android.Views.InputMethods;
using osu.Framework.Input;

namespace osu.Framework.Android.Input
{
    public class AndroidTextInput : ITextInputSource
    {
        private readonly AndroidGameView view;
        private readonly AndroidGameActivity activity;
        private readonly InputMethodManager inputMethodManager;

        public event Action<string> OnTextInput;

        public bool Active { get; private set; }

        public AndroidTextInput(AndroidGameView view)
        {
            this.view = view;
            activity = (AndroidGameActivity)view.Context;

            inputMethodManager = view.Context.GetSystemService(Context.InputMethodService) as InputMethodManager;
        }

        private void commitText(string text)
        {
            if (Active)
                OnTextInput?.Invoke(text);
        }

        private void keyDown(Keycode arg, KeyEvent e)
        {
            if (Active && e.UnicodeChar != 0)
                OnTextInput?.Invoke(((char)e.UnicodeChar).ToString());
        }

        public void Activate()
        {
            activity.RunOnUiThread(() =>
            {
                view.RequestFocus();
                inputMethodManager.ShowSoftInput(view, 0);
                view.KeyDown += keyDown;
                view.CommitText += commitText;
            });
            Active = true;
        }

        public void Deactivate()
        {
            activity.RunOnUiThread(() =>
            {
                inputMethodManager.HideSoftInputFromWindow(view.WindowToken, HideSoftInputFlags.None);
                view.ClearFocus();
                view.KeyDown -= keyDown;
                view.CommitText -= commitText;
            });
            Active = false;
        }

        public void EnsureActivated()
        {
            activity.RunOnUiThread(() =>
            {
                inputMethodManager.ShowSoftInput(view, 0);
            });
        }
    }
}
