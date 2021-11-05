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
        private string pending = string.Empty;
        private readonly object pendingLock = new object();

        public AndroidTextInput(AndroidGameView view)
        {
            this.view = view;
            activity = (AndroidGameActivity)view.Context;

            if (view.Context != null)
                inputMethodManager = view.Context.GetSystemService(Context.InputMethodService) as InputMethodManager;
        }

        public bool ImeActive => false;

        public string GetPendingText()
        {
            lock (pendingLock)
            {
                string oldPending = pending;
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

        public void Activate()
        {
            view.KeyDown += keyDown;
            view.CommitText += commitText;

            activity.RunOnUiThread(() =>
            {
                view.RequestFocus();
                inputMethodManager?.ShowSoftInput(view, 0);
            });
        }

        public void EnsureActivated()
        {
            activity.RunOnUiThread(() =>
            {
                view.RequestFocus();
                inputMethodManager?.ShowSoftInput(view, 0);
            });
        }

        public void Deactivate()
        {
            view.KeyDown -= keyDown;
            view.CommitText -= commitText;

            activity.RunOnUiThread(() =>
            {
                inputMethodManager?.HideSoftInputFromWindow(view.WindowToken, HideSoftInputFlags.None);
                view.ClearFocus();
            });
        }

        public event Action<string> OnNewImeComposition;
        public event Action<string> OnNewImeResult;
    }
}
