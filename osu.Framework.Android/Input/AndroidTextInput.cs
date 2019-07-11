// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Android.Content;
using Android.Views;
using Android.Views.InputMethods;
using Android.Util;
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

        public void Deactivate(object sender)
        {
            inputMethodManager.HideSoftInputFromWindow(view.WindowToken, HideSoftInputFlags.None);
            view.KeyDown -= keyDown;
            view.CommitText -= commitText;
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

            if (upper)
            {
                char toAdd;
                switch (arg)
                {
                    case Keycode.Grave:
                        toAdd = '~';
                        break;

                    case Keycode.Minus:
                        toAdd = '_';
                        break;

                    case Keycode.Equals:
                        toAdd = '+';
                        break;

                    case Keycode.LeftBracket:
                        toAdd = '{';
                        break;

                    case Keycode.RightBracket:
                        toAdd = '}';
                        break;

                    case Keycode.Backslash:
                        toAdd = '|';
                        break;

                    case Keycode.Apostrophe:
                        toAdd = '"';
                        break;

                    case Keycode.Semicolon:
                        toAdd = ':';
                        break;

                    case Keycode.Slash:
                        toAdd = '?';
                        break;

                    case Keycode.Period:
                        toAdd = '>';
                        break;

                    case Keycode.Comma:
                        toAdd = '<';
                        break;

                    default:
                        toAdd = e.DisplayLabel;
                        break;
                }

                pending += toAdd;
            }
            else
                pending += e.DisplayLabel.ToString().ToLower();
        }

        public bool ImeActive => false;

        public event Action<string> OnNewImeComposition;
        public event Action<string> OnNewImeResult;

        public void Activate(object sender)
        {
            inputMethodManager.ToggleSoftInput(ShowFlags.Forced, HideSoftInputFlags.None);
            view.KeyDown += keyDown;
            view.CommitText += commitText;
        }
    }
}
