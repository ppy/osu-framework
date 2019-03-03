// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Android.Content;
using Android.Views;
using Android.Views.InputMethods;
using osu.Framework.Input;
using osuTK.Input;
using System;
using System.Linq;

namespace osu.Framework.Android.Input
{
    public class AndroidTextInput : ITextInputSource
    {
        private readonly AndroidGameView view;
        private readonly InputMethodManager inputMethodManager;
        private string pending = string.Empty;
        private readonly object pendingLock = new object();

        public AndroidTextInput(AndroidGameView view)
        {
            this.view = view;

            inputMethodManager = view.Context.GetSystemService(Context.InputMethodService) as InputMethodManager;
        }

        private void keyDown(Keycode arg, KeyEvent e)
        {
            Key key = AndroidKeyboardHandler.GetKeyCodeAsKey(arg);
            string keynum = arg.ToString();
            bool upper = e.IsShiftPressed;

            if (keynum.StartsWith(Keycode.Num.ToString()))
            {
                switch (keynum.Last())
                {
                    case '1':
                        pending = upper ? "!" : "1";
                        return;
                    case '2':
                        pending = upper ? "@" : "2";
                        return;
                    case '3':
                        pending = upper ? "#" : "3";
                        return;
                    case '4':
                        pending = upper ? "$" : "4";
                        return;
                    case '5':
                        pending = upper ? "%" : "5";
                        return;
                    case '6':
                        pending = upper ? "^" : "6";
                        return;
                    case '7':
                        pending = upper ? "&" : "7";
                        return;
                    case '8':
                        pending = upper ? "*" : "8";
                        return;
                    case '9':
                        pending = upper ? "(" : "9";
                        return;
                    case '0':
                        pending = upper ? ")" : "0";
                        return;
                    default:
                        pending = "" + keynum.Last();
                        return;
                }
            }

            switch (arg)
            {
                default:
                    pending += upper ? e.DisplayLabel.ToString() : e.DisplayLabel.ToString().ToLower();
                    break;
            }
        }

        public bool ImeActive => false;

        public event Action<string> OnNewImeComposition;
        public event Action<string> OnNewImeResult;

        public void Activate(object sender)
        {
            inputMethodManager.ToggleSoftInput(ShowFlags.Forced, HideSoftInputFlags.None);
            view.KeyDown += keyDown;
        }

        public void Deactivate(object sender)
        {
            inputMethodManager.HideSoftInputFromWindow(view.WindowToken, HideSoftInputFlags.None);
            view.KeyDown -= keyDown;
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
    }
}
