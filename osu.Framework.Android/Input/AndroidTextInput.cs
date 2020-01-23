// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Android.Content;
using Android.Views;
using Android.Views.InputMethods;
using osu.Framework.Input;
using System;

namespace osu.Framework.Android.Input
{
    public class AndroidTextInput : ITextInputSource
    {
        private readonly AndroidGameView view;
        private readonly AndroidGameActivity activity;
        private readonly InputMethodManager inputMethodManager;
        private string pending = string.Empty;
        private readonly object pendingLock = new object();

        public AndroidTextInput(AndroidGameView view)
        {
            this.view = view;
            activity = (AndroidGameActivity)view.Context;

            inputMethodManager = view.Context.GetSystemService(Context.InputMethodService) as InputMethodManager;
        }

        public void Deactivate(object sender)
        {
            activity.RunOnUiThread(() =>
            {
                inputMethodManager.HideSoftInputFromWindow(view.WindowToken, HideSoftInputFlags.None);
                view.ClearFocus();
                view.KeyDown -= keyDown;
                view.CommitText -= commitText;
            });
        }

        public string GetPendingText()
        {
            lock (pendingLock)
            {
                var oldPending = pending;
                pending = string.Empty;
                return oldPending;
            }
        }

        private void commitText(string text)
        {
            OnNewImeComposition?.Invoke(text);
            OnNewImeResult?.Invoke(text);
        }

        private void keyDown(Keycode arg, KeyEvent e)
        {
            if (e.UnicodeChar != 0)
                pending += (char)e.UnicodeChar;
        }

        public bool ImeActive => false;

        public event Action<string> OnNewImeComposition;
        public event Action<string> OnNewImeResult;

        public void Activate(object sender)
        {
            activity.RunOnUiThread(() =>
            {
                view.RequestFocus();
                inputMethodManager.ToggleSoftInputFromWindow(view.WindowToken, ShowSoftInputFlags.Forced, HideSoftInputFlags.None);
                view.KeyDown += keyDown;
                view.CommitText += commitText;
            });
        }
    }
}
