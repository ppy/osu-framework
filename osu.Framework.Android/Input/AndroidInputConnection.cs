// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.Views;
using Android.Views.InputMethods;
using Android.Util;
using Java.Lang;
using osu.Framework.Android;

namespace osu.Framework.Android.Input
{
    class AndroidInputConnection : BaseInputConnection
    {
        static readonly Dictionary<char, char> shiftKeycodeMap = new Dictionary<char, char>
        {
            { '~', '`' }, { '!', '1' }, { '@', '2' }, { '#', '3' }, { '$', '4' },
            { '%', '5' }, { '^', '6' }, { '&', '7' }, { '*', '8' }, { '(', '9' },
            { ')', '0' }, { '_', '-' }, { '+', '=' }, { '{', '[' }, { '}', ']' },
            { '|', '\\' }, { ':', ';' }, { '"', '\'' }, { '?', '/' }, { '>', '.' },
            { '<', ',' }
        };

        static readonly Dictionary<char, Keycode> charToKeycodeMap = new Dictionary<char, Keycode>
        {
            { '+',  Keycode.Plus},
                { '-',  Keycode.Minus},
                { '*',  Keycode.Star},
                { '/',  Keycode.Slash},
                { '=',  Keycode.Equals},
                { '@',  Keycode.At},
                { '#',  Keycode.Pound},
                { '\'',  Keycode.Apostrophe},
                { '.',  Keycode.Period},
                { '[',  Keycode.LeftBracket},
                { ']',  Keycode.RightBracket},
                { ';',  Keycode.Semicolon},
                { '`',  Keycode.Grave},
                { ' ',  Keycode.Space},
                { ',',  Keycode.Comma},
        };

        public AndroidGameView TargetView { get; set; }

        public AndroidInputConnection(AndroidGameView targetView, bool fullEditor) : base(targetView, fullEditor)
        {
            TargetView = targetView;
        }

        public override bool CommitText(ICharSequence text, int newCursorPosition)
        {
            Log.Info("osu!lazer", "CommitText " + text);
            if (text.Length() != 0)
            {
                //TargetView?.OnCommitText(text.ToString());
                //direct commit some text is not supported by framework now, so we convert the input text to key events.
                foreach (char c in text.ToArray())
                {
                    bool needShift = NeedShiftPress(c);
                    Keycode keycode = TryParseCharIntoKeycode(ToUnshiftChar(c));
                    if (needShift)
                    {
                        SendKeyEvent(new KeyEvent(0, 0, KeyEventActions.Down, Keycode.ShiftLeft, 0, MetaKeyStates.ShiftOn | MetaKeyStates.ShiftLeftOn));
                        SendKeyEvent(new KeyEvent(0, 0, KeyEventActions.Down, keycode, 0, MetaKeyStates.ShiftOn | MetaKeyStates.ShiftLeftOn));
                        SendKeyEvent(new KeyEvent(0, 0, KeyEventActions.Up, keycode, 0, MetaKeyStates.ShiftOn | MetaKeyStates.ShiftLeftOn));
                        SendKeyEvent(new KeyEvent(0, 0, KeyEventActions.Up, Keycode.ShiftLeft, 0, MetaKeyStates.ShiftOn | MetaKeyStates.ShiftLeftOn));
                    }
                    else
                    {
                        SendKeyEvent(new KeyEvent(KeyEventActions.Down, keycode));
                        SendKeyEvent(new KeyEvent(KeyEventActions.Up, keycode));
                    }
                }
                //KeyEvent ed = new KeyEvent(0, text.ToString(), 1, 0);
                //SendKeyEvent(ed);
                return true;
            }
            return base.CommitText(text, newCursorPosition);
        }

        public override bool SendKeyEvent(KeyEvent e)
        {
            //Log.Info("osu!lazer", "SendKeyEvent " + e);
            switch (e.Action)
            {
                case KeyEventActions.Down:
                    TargetView?.OnKeyDown(e.KeyCode, e);
                    return true;
                case KeyEventActions.Up:
                    TargetView?.OnKeyUp(e.KeyCode, e);
                    return true;
                case KeyEventActions.Multiple:
                    TargetView?.OnKeyDown(e.KeyCode, e);
                    TargetView?.OnKeyUp(e.KeyCode, e);
                    return true;
            }
            return base.SendKeyEvent(e);
        }

        public override bool DeleteSurroundingText(int beforeLength, int afterLength)
        {
            for (int i = 0; i < beforeLength; i++)
            {
                KeyEvent ed = new KeyEvent(KeyEventActions.Multiple, Keycode.Del);
                SendKeyEvent(ed);
            }
            return true;
        }

        public static Keycode TryParseCharIntoKeycode(char c)
        {
            if (c >= 'A' && c <= 'Z')
            {
                return Keycode.A + (c - 'A');
            }
            if (c >= 'a' && c <= 'z')
            {
                return Keycode.A + (c - 'a');
            }
            if (c >= '0' && c <= '9')
            {
                return Keycode.Num0 + (c - '0');
            }
            return parseSymbolCharToKeycode(c);
        }

        private static Keycode parseSymbolCharToKeycode(char c)
        {
            switch (c)
            {
                case '+': return Keycode.Plus;
                case '-': return Keycode.Minus;
                case '*': return Keycode.Star;
                case '/': return Keycode.Slash;
                case '=': return Keycode.Equals;
                case '@': return Keycode.At;
                case '#': return Keycode.Pound;
                case '\'': return Keycode.Apostrophe;
                case '.': return Keycode.Period;
                case '[': return Keycode.LeftBracket;
                case ']': return Keycode.RightBracket;
                case ';': return Keycode.Semicolon;
                case '`': return Keycode.Grave;
                case ' ': return Keycode.Space;
                case ',': return Keycode.Comma;
                default: return Keycode.Unknown;
            }
        }

        public static bool NeedShiftPress(char c)
        {
            if (c >= 'A' && c <= 'Z')
            {
                return true;
            }
            if (c >= 'a' && c <= 'z')
            {
                return false;
            }
            if (c >= '0' && c <= '9')
            {
                return false;
            }
            return shiftKeycodeMap.ContainsKey(c);
        }

        public static char ToUnshiftChar(char c)
        {
            if (c >= 'A' && c <= 'Z')
            {
                return (char)('a' + (c - 'A'));
            }
            return shiftKeycodeMap.ContainsKey(c) ? shiftKeycodeMap[c] : c;
        }
    }
}
