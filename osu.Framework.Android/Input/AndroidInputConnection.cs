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
            { '+', Keycode.Plus }, { '-', Keycode.Minus }, { '*', Keycode.Star },
            { '/', Keycode.Slash }, { '=', Keycode.Equals }, { '@', Keycode.At },
            { '#', Keycode.Pound }, { '\'', Keycode.Apostrophe }, { '.', Keycode.Period },
            { '[', Keycode.LeftBracket }, { ']', Keycode.RightBracket }, { ';', Keycode.Semicolon },
            { '`', Keycode.Grave }, { ' ', Keycode.Space }, { ',', Keycode.Comma }
        };

        public AndroidGameView TargetView { get; set; }

        public AndroidInputConnection(AndroidGameView targetView, bool fullEditor) : base(targetView, fullEditor)
        {
            TargetView = targetView;
        }

        public override bool CommitText(ICharSequence text, int newCursorPosition)
        {
            if (text.Length() != 0)
            {
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

                return true;
            }

            return base.CommitText(text, newCursorPosition);
        }

        public override bool SendKeyEvent(KeyEvent e)
        {
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
            if(char.IsLetter(c))
                return Keycode.A + (char.ToLower(c) - 'a');

            if (char.IsDigit(c))
                return Keycode.Num0 + (c - '0');

            return charToKeycodeMap.ContainsKey(c) ? charToKeycodeMap[c] : Keycode.Unknown;
        }

        public static bool NeedShiftPress(char c)
        {
            if (char.IsLetterOrDigit(c))
                return char.IsUpper(c);

            return shiftKeycodeMap.ContainsKey(c);
        }

        public static char ToUnshiftChar(char c)
        {
            if (char.IsLetter(c))
                return char.ToLower(c);

            return shiftKeycodeMap.ContainsKey(c) ? shiftKeycodeMap[c] : c;
        }
    }
}
