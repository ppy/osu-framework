// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace osu.Framework.Lists
{
    public readonly struct SlimReadOnlyDictionaryWrapper<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>
        where TKey : notnull
    {
        private readonly Dictionary<TKey, TValue> dict;

        public SlimReadOnlyDictionaryWrapper(Dictionary<TKey, TValue> dict)
        {
            this.dict = dict;
        }

        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
            => dict.GetEnumerator();

        public Dictionary<TKey, TValue>.Enumerator GetEnumerator()
            => dict.GetEnumerator();

        public Dictionary<TKey, TValue>.KeyCollection Keys
            => dict.Keys;

        public Dictionary<TKey, TValue>.ValueCollection Values
            => dict.Values;

        public bool ContainsKey(TKey key)
            => dict.ContainsKey(key);

        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
            => dict.TryGetValue(key, out value);

        public TValue this[TKey key]
            => dict[key];

        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys
            => dict.Keys;

        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values
            => dict.Values;

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        public int Count
            => dict.Count;
    }
}
