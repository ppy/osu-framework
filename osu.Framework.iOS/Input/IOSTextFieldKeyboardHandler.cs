// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Globalization;
using System.Linq;
using Foundation;
using osu.Framework.Input.Handlers;
using osu.Framework.Input.StateChanges;
using osu.Framework.Platform;
using osuTK.Input;

namespace osu.Framework.iOS.Input
{
    public class IOSTextFieldKeyboardHandler : InputHandler
    {
        private readonly IOSGameView view;

        public IOSTextFieldKeyboardHandler(IOSGameView view)
        {
            this.view = view;
            view.KeyboardTextField.HandleShouldChangeCharacters += handleShouldChangeCharacters;
            view.KeyboardTextField.HandleShouldReturn += handleShouldReturn;
        }

        /// <summary>
        /// Whether the specified key has been handled by this handler.
        /// </summary>
        public bool Handled(Key key) => PendingInputs.OfType<KeyboardKeyInput>().Any(i => i.Entries.Any(e => e.Button == key));

        private void handleShouldChangeCharacters(NSRange range, string text)
        {
            if (!IsActive)
                return;

            if (text.Length == 0)
            {
                Key key = range.Location < IOSGameView.HiddenTextField.CURSOR_POSITION ? Key.BackSpace : Key.Delete;

                // NOTE: this makes the assumption that Key.AltLeft triggers the WordPrevious platform action
                if (range.Length > 1)
                    PendingInputs.Enqueue(new KeyboardKeyInput(Key.AltLeft, true));

                if (range.Length > 0)
                {
                    PendingInputs.Enqueue(new KeyboardKeyInput(key, true));
                    PendingInputs.Enqueue(new KeyboardKeyInput(key, false));
                }

                if (range.Length > 1)
                    PendingInputs.Enqueue(new KeyboardKeyInput(Key.AltLeft, false));

                return;
            }

            foreach (char c in text)
            {
                Key? key = keyForString(char.ToString(c), out bool upper);

                if (!key.HasValue) continue;

                if (upper)
                    PendingInputs.Enqueue(new KeyboardKeyInput(Key.LShift, true));

                PendingInputs.Enqueue(new KeyboardKeyInput(key.Value, true));
                PendingInputs.Enqueue(new KeyboardKeyInput(key.Value, false));

                if (upper)
                    PendingInputs.Enqueue(new KeyboardKeyInput(Key.LShift, false));
            }
        }

        private void handleShouldReturn()
        {
            if (!IsActive)
                return;

            PendingInputs.Enqueue(new KeyboardKeyInput(Key.Enter, true));
            PendingInputs.Enqueue(new KeyboardKeyInput(Key.Enter, false));
        }

        private Key? keyForString(string str, out bool upper)
        {
            upper = false;
            if (str.Length == 0)
                return null;

            char c = str[0];

            switch (c)
            {
                case ' ':
                    return Key.Space;

                case '\t':
                    return Key.Tab;

                case '1':
                case '!':
                    upper = !char.IsDigit(c);
                    return Key.Number1;

                case '2':
                case '@':
                    upper = !char.IsDigit(c);
                    return Key.Number2;

                case '3':
                case '#':
                    upper = !char.IsDigit(c);
                    return Key.Number3;

                case '4':
                case '$':
                    upper = !char.IsDigit(c);
                    return Key.Number4;

                case '5':
                case '%':
                    upper = !char.IsDigit(c);
                    return Key.Number5;

                case '6':
                case '^':
                    upper = !char.IsDigit(c);
                    return Key.Number6;

                case '7':
                case '&':
                    upper = !char.IsDigit(c);
                    return Key.Number7;

                case '8':
                case '*':
                    upper = !char.IsDigit(c);
                    return Key.Number8;

                case '9':
                case '(':
                    upper = !char.IsDigit(c);
                    return Key.Number9;

                case '0':
                case ')':
                    upper = !char.IsDigit(c);
                    return Key.Number0;

                case '-':
                case '_':
                    upper = c == '_';
                    return Key.Minus;

                case '=':
                case '+':
                    upper = c == '+';
                    return Key.Plus;

                case '`':
                case '~':
                    upper = c == '~';
                    return Key.Tilde;

                case '[':
                case '{':
                    upper = c == '{';
                    return Key.BracketLeft;

                case ']':
                case '}':
                    upper = c == '}';
                    return Key.BracketRight;

                case '\\':
                case '|':
                    upper = c == '|';
                    return Key.BackSlash;

                case ';':
                case ':':
                    upper = c == ':';
                    return Key.Semicolon;

                case '\'':
                case '\"':
                    upper = c == '\"';
                    return Key.Quote;

                case ',':
                case '<':
                    upper = c == '<';
                    return Key.Comma;

                case '.':
                case '>':
                    upper = c == '>';
                    return Key.Period;

                case '/':
                case '?':
                    upper = c == '?';
                    return Key.Slash;

                default:
                    if (char.IsLetter(c))
                    {
                        string keyName = c.ToString().ToUpper(CultureInfo.CurrentCulture);
                        if (Enum.TryParse(keyName, out Key result))
                            return result;
                    }

                    return null;
            }
        }

        internal bool KeyboardActive;
        public override bool IsActive => KeyboardActive;

        protected override void Dispose(bool disposing)
        {
            view.KeyboardTextField.HandleShouldChangeCharacters -= handleShouldChangeCharacters;
            view.KeyboardTextField.HandleShouldReturn -= handleShouldReturn;
            base.Dispose(disposing);
        }

        public override bool Initialize(GameHost host) => true;
    }
}
