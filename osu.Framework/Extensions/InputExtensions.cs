// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Input;
using osuTK.Input;

namespace osu.Framework.Extensions
{
    public static class InputExtensions
    {
        /// <summary>
        /// Gets a default character associated with this <see cref="Key"/>.
        /// </summary>
        /// <param name="key">A keyboard key</param>
        /// <returns>The <see cref="KeyboardKey.Character"/> this <paramref name="key"/> would produce on a en-us keyboard layout.</returns>
        public static char GetDefaultCharacter(this Key key)
        {
            static bool inBetween(Key key, Key first, Key last, char firstCharacter, out char result)
            {
                if (key >= first && key <= last)
                {
                    result = (char)(key - first + firstCharacter);
                    return true;
                }

                result = default;
                return false;
            }

            if (inBetween(key, Key.Keypad0, Key.Keypad9, '0', out char result))
                return result;

            if (inBetween(key, Key.A, Key.Z, 'a', out result))
                return result;

            if (inBetween(key, Key.Number0, Key.Number9, '0', out result))
                return result;

            switch (key)
            {
                default:
                    return default;

                case Key.Enter:
                case Key.KeypadEnter:
                    return '\n';

                case Key.Escape:
                    return '\x1b';

                case Key.Space:
                    return ' ';

                case Key.Tab:
                    return '\t';

                case Key.BackSpace:
                    return '\b';

                case Key.Delete:
                    return '\x7f';

                case Key.KeypadDivide:
                case Key.Slash:
                    return '/';

                case Key.KeypadMultiply:
                    return '*';

                case Key.KeypadMinus:
                case Key.Minus:
                    return '-';

                case Key.Plus:
                case Key.KeypadPlus:
                    return '+';

                case Key.Period:
                case Key.KeypadPeriod:
                    return '.';

                case Key.Tilde:
                    return '~';

                case Key.BracketLeft:
                    return '[';

                case Key.BracketRight:
                    return ']';

                case Key.Semicolon:
                    return ';';

                case Key.Quote:
                    return '\'';

                case Key.Comma:
                    return ',';

                case Key.BackSlash:
                case Key.NonUSBackSlash:
                    return '\\';
            }
        }
    }
}
