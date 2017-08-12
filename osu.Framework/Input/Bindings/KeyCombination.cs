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
        public KeyCombination(string keys) : this(keys.Split(',').Select(s => (Key)int.Parse(s)))
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
    }
}