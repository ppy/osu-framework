// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK.Input;

namespace osu.Framework.Input.Bindings
{
    /// <summary>
    /// Represent a combination of more than one <see cref="Key"/>s.
    /// </summary>
    public class KeyCombination : IEquatable<KeyCombination>
    {
        /// <summary>
        /// The keys.
        /// </summary>
        public readonly IEnumerable<Key> Keys;

        /// <summary>
        /// Construct a new instance.
        /// </summary>
        /// <param name="keys">The keys.</param>
        public KeyCombination(IEnumerable<Key> keys)
        {
            Keys = keys.OrderBy(k => (int)k).ToArray();
        }

        /// <summary>
        /// Construct a new instance.
        /// </summary>
        /// <param name="keys">A comma-separated (KeyCode) string representation of the keys.</param>
        public KeyCombination(string keys)
            : this(keys.Split(',').Select(s => (Key)int.Parse(s)))
        {
        }

        /// <summary>
        /// Check whether the provided input is a valid pressedKeys for this combination.
        /// </summary>
        /// <param name="pressedKeys">The potential pressedKeys for this combination.</param>
        /// <returns>Whether the pressedKeys keys are valid.</returns>
        public bool IsPressed(IEnumerable<Key> pressedKeys) => !Keys.Except(pressedKeys).Any();

        public bool Equals(KeyCombination other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Keys.SequenceEqual(other.Keys);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((KeyCombination)obj);
        }

        public override int GetHashCode() => Keys != null ? Keys.Select(k => k.GetHashCode()).Aggregate((h1, h2) => h1 * 17 + h2) : 0;

        public static implicit operator KeyCombination(Key singleKey) => new KeyCombination(new[] { singleKey });

        public static implicit operator KeyCombination(string stringRepresentation) => new KeyCombination(stringRepresentation);

        public static implicit operator KeyCombination(Key[] keys) => new KeyCombination(keys);

        public override string ToString() => Keys?.Select(k => ((int)k).ToString()).Aggregate((s1, s2) => $"{s1},{s2}") ?? string.Empty;

        public string ReadableString() => Keys?.Select(getReadableKey).Aggregate((s1, s2) => $"{s1}+{s2}") ?? string.Empty;

        private string getReadableKey(Key key)
        {
            switch (key)
            {
                case Key.Unknown:
                    return string.Empty;
                case Key.ShiftLeft:
                    return "LShift";
                case Key.ShiftRight:
                    return "RShift";
                case Key.ControlLeft:
                    return "LCtrl";
                case Key.ControlRight:
                    return "RCtrl";
                case Key.AltLeft:
                    return "LAlt";
                case Key.AltRight:
                    return "RAlt";
                case Key.WinLeft:
                    return "LWin";
                case Key.WinRight:
                    return "RWin";
                case Key.Escape:
                    return "Esc";
                case Key.BackSpace:
                    return "Backsp";
                case Key.Insert:
                    return "Ins";
                case Key.Delete:
                    return "Del";
                case Key.PageUp:
                    return "Pgup";
                case Key.PageDown:
                    return "Pgdn";
                case Key.CapsLock:
                    return "Caps";
                case Key.Number0:
                case Key.Keypad0:
                    return "0";
                case Key.Number1:
                case Key.Keypad1:
                    return "1";
                case Key.Number2:
                case Key.Keypad2:
                    return "2";
                case Key.Number3:
                case Key.Keypad3:
                    return "3";
                case Key.Number4:
                case Key.Keypad4:
                    return "4";
                case Key.Number5:
                case Key.Keypad5:
                    return "5";
                case Key.Number6:
                case Key.Keypad6:
                    return "6";
                case Key.Number7:
                case Key.Keypad7:
                    return "7";
                case Key.Number8:
                case Key.Keypad8:
                    return "8";
                case Key.Number9:
                case Key.Keypad9:
                    return "9";
                case Key.Tilde:
                    return "~";
                case Key.Minus:
                    return "-";
                case Key.Plus:
                    return "+";
                case Key.BracketLeft:
                    return "(";
                case Key.BracketRight:
                    return ")";
                case Key.Semicolon:
                    return ";";
                case Key.Quote:
                    return "\"";
                case Key.Comma:
                    return ",";
                case Key.Period:
                    return ".";
                case Key.Slash:
                    return "/";
                case Key.BackSlash:
                case Key.NonUSBackSlash:
                    return "\\";
                default:
                    return key.ToString();
            }
        }
    }
}
