// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

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

        public AndroidTextInput(AndroidGameView view)
        {
            this.view = view;

            inputMethodManager = view.Context.GetSystemService(Context.InputMethodService) as InputMethodManager;
        }

        private void keyDown(Keycode arg)
        {
            Key key = AndroidKeyboardHandler.GetKeyCodeAsKey(arg);
            string keynum = arg.ToString();

            if (keynum.StartsWith(Keycode.Num.ToString()))
            {
                pending = "" + keynum.Last();
                return;
            }

            switch (arg)
            {
                case Keycode.Space:
                    pending += " ";
                    break;
                case Keycode.LeftBracket:
                    pending += "[";
                    break;
                case Keycode.RightBracket:
                    pending += "]";
                    break;
                case Keycode.Backslash:
                    pending += "\\";
                    break;
                case Keycode.Apostrophe:
                    pending += "'";
                    break;
                case Keycode.Plus:
                case Keycode.NumpadAdd:
                    pending += "+";
                    break;
                case Keycode.Minus:
                case Keycode.NumpadSubtract:
                    pending += "-";
                    break;
                case Keycode.Star:
                case Keycode.NumpadMultiply:
                    pending += "*";
                    break;
                case Keycode.Slash:
                case Keycode.NumpadDivide:
                    pending += "/";
                    break;
                case Keycode.Period:
                    pending += ".";
                    break;
                case Keycode.Comma:
                    pending += ",";
                    break;
                case Keycode.Grave:
                    pending += "`";
                    break;
                case Keycode.Semicolon:
                    pending += ";";
                    break;
                case Keycode.CtrlLeft:
                case Keycode.AltLeft:
                case Keycode.ShiftLeft:
                case Keycode.CtrlRight:
                case Keycode.AltRight:
                case Keycode.ShiftRight:
                case Keycode.CapsLock:
                case Keycode.Tab:
                case Keycode.F1:
                case Keycode.F2:
                case Keycode.F3:
                case Keycode.F4:
                case Keycode.F5:
                case Keycode.F6:
                case Keycode.F7:
                case Keycode.F8:
                case Keycode.F9:
                case Keycode.F10:
                case Keycode.F11:
                case Keycode.F12:
                case Keycode.NumLock:
                case Keycode.ScrollLock:
                case Keycode.DpadDown:
                case Keycode.DpadLeft:
                case Keycode.DpadRight:
                case Keycode.DpadUp:
                case Keycode.PageDown:
                case Keycode.PageUp:
                case Keycode.Home:
                case Keycode.MoveEnd:
                case Keycode.Del:
                case Keycode.ForwardDel:
                case Keycode.Insert:
                case Keycode.NumpadEnter:
                case Keycode.Enter:
                    break;
                default:
                    pending += key.ToString();
                    break;
            }
        }

        public bool ImeActive
            => false;

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
            try
            {
                return pending;
            }
            finally
            {
                pending = string.Empty;
            }
        }
    }
}
