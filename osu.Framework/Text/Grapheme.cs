// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace osu.Framework.Text
{
    /// <summary>
    /// Represents a single grapheme cluster in Unicode text. A grapheme cluster is what users perceive as a single character,
    /// which can be composed of one or more Unicode code points. This struct efficiently handles both simple BMP characters
    /// and complex multi-character sequences like emoji with skin tone modifiers or characters with combining marks.
    /// </summary>
    /// <remarks>
    /// This implementation optimizes for the common case of single BMP characters by storing them directly as a char,
    /// while falling back to string storage for more complex grapheme clusters. This approach minimizes memory allocations
    /// and provides better performance for typical text processing scenarios.
    ///
    /// Comparison of <see cref="char"/>, <see cref="Rune"/>, and <see cref="Grapheme"/>:
    /// <code>
    /// string text = "Hello 👋🏽 café!";
    ///
    /// // Different counting methods:
    /// Console.WriteLine($"String.Length: {text.Length}");           // 13 (UTF-16 code units)
    /// Console.WriteLine($"Rune count: {text.EnumerateRunes().Count()}"); // 11 (Unicode code points)
    /// Console.WriteLine($"Grapheme count: {Grapheme.GetGraphemeEnumerator(text).Count()}"); // 10 (user-perceived characters)
    ///
    /// // Why the difference?
    /// // '👋🏽' = 1 grapheme, but 2 runes (👋 + 🏽), and 4 UTF-16 code units
    /// // 'é' might be 1 grapheme, 1 or 2 runes (precomposed vs base+combining), 1 or 2 UTF-16 units
    ///
    /// // Processing each type:
    /// foreach (char c in text) { /* processes UTF-16 code units */ }
    /// foreach (Rune r in text.EnumerateRunes()) { /* processes Unicode code points */ }
    /// foreach (Grapheme g in Grapheme.GetGraphemeEnumerator(text)) { /* processes user-perceived characters */ }
    /// </code>
    /// </remarks>
    public readonly struct Grapheme : IEquatable<Grapheme>
    {
        /// <summary>
        /// String representation for multi-character grapheme clusters (null for single BMP characters).
        /// Used for complex Unicode sequences like surrogate pairs, emoji with modifiers, or combining character sequences.
        /// </summary>
        private string? str { get; init; }

        /// <summary>
        /// Single character value for BMP characters, or the first character/replacement character for complex sequences.
        /// For surrogate pairs, this contains the replacement character (U+FFFD) to avoid invalid char values.
        /// </summary>
        private char c { get; init; }

        /// <summary>
        /// Gets the character value for single BMP characters.
        /// For multi-character sequences, returns the first character or a replacement character for surrogate pairs.
        /// </summary>
        public char CharValue => c;

        /// <summary>
        /// Gets the string representation for multi-character grapheme clusters.
        /// Returns null for single BMP characters that are stored efficiently as a char.
        /// </summary>
        public string? StringValue => str;

        /// <summary>
        /// Gets the length of the UTF-16 sequence representing this grapheme.
        /// Returns 1 for single BMP characters, or the actual string length for complex sequences.
        /// </summary>
        public int Utf16SequenceLength => str?.Length ?? 1;

        /// <summary>
        /// Gets a value indicating whether this grapheme represents a single character in the Basic Multilingual Plane (BMP).
        /// BMP characters can be represented by a single 16-bit char value (U+0000 to U+FFFF).
        /// </summary>
        public bool IsBmp => str == null;

        /// <summary>
        /// Gets a value indicating whether this grapheme represents a Unicode surrogate pair.
        /// Surrogate pairs are used to represent characters outside the BMP (U+10000 to U+10FFFF) using two 16-bit values.
        /// </summary>
        public bool IsSurrogate => str != null && str.Length == 2 && str[0] >= 0xD800U && str[0] <= 0xDBFFU && str[1] >= 0xDC00U && str[1] <= 0xDFFFU;

        /// <summary>
        /// Gets a value indicating whether this grapheme represents a single Unicode scalar value.
        /// This includes both BMP characters and surrogate pairs, but excludes complex grapheme clusters
        /// with multiple scalar values (like emoji with modifiers or combining characters).
        /// </summary>
        public bool IsSingleScalarValue => IsBmp || IsSurrogate;

        /// <summary>
        /// Determines whether this grapheme represents a whitespace character.
        /// Currently only supports detection for single BMP characters.
        /// </summary>
        /// <returns>true if the grapheme is a whitespace character; otherwise, false.</returns>
        public bool IsWhiteSpace()
        {
            if (IsBmp)
                return char.IsWhiteSpace(c);

            // We don't support whitespace detection for multi-character sequences
            return false;
        }

        public static explicit operator Grapheme(char ch) => new Grapheme(ch);

        public static explicit operator Grapheme(Rune rune) => new Grapheme(rune.ToString());

        public static explicit operator Rune(Grapheme grapheme)
        {
            if (grapheme.str != null && grapheme.str.Length >= 2 && grapheme.str[0] >= 0xD800U && grapheme.str[0] <= 0xDBFFU)
            {
                return new Rune(grapheme.str[0], grapheme.str[1]);
            }

            return new Rune(grapheme.c);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Grapheme"/> struct with a single character.
        /// </summary>
        /// <param name="c">The character to represent.</param>
        public Grapheme(char c)
        {
            this.c = c;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Grapheme"/> struct with a string.
        /// </summary>
        /// <param name="str">The string representing the grapheme cluster.</param>
        public Grapheme(string str)
        {
            if (str.Length == 1)
            {
                // For single characters, we can store them directly in the char field
                c = str[0];
            }
            else
            {
                this.str = str;
                // For surrogate pairs, use replacement character to avoid storing invalid high surrogate in char field
                if (!(str[0] >= 0xD800U && str[0] <= 0xDBFFU))
                    c = str[0];
                else
                    c = (char)0xFFFDU; // Unicode replacement character
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Grapheme"/> struct from a ReadOnlySpan of characters.
        /// </summary>
        /// <param name="span">The character span representing the grapheme cluster.</param>
        public Grapheme(ReadOnlySpan<char> span)
        {
            if (span.Length == 1)
            {
                c = span[0];
            }
            else
            {
                str = span.ToString();
                // For surrogate pairs, use replacement character to avoid storing invalid high surrogate in char field
                if (!(str[0] >= 0xD800U && str[0] <= 0xDBFFU))
                    c = str[0];
                else
                    c = (char)0xFFFDU; // Unicode replacement character
            }
        }

        public static bool operator ==(Grapheme left, Grapheme right) => left.c == right.c && left.str == right.str;

        public static bool operator !=(Grapheme left, Grapheme right) => left.c != right.c || left.str != right.str;

        /// <summary>
        /// Attempts to remove the last modifier (skin tone, combining mark, etc.) for font fallback.
        /// Returns null if no modifier can be removed.
        ///
        /// <example>
        /// <code>
        /// var emojiWithSkinTone = new Grapheme("👋🏽"); // Waving hand + medium skin tone
        /// var baseEmoji = emojiWithSkinTone.RemoveLastModifier();
        /// Console.WriteLine(baseEmoji); // 👋 // Waving hand
        /// </code>
        /// </example>
        /// </summary>
        public Grapheme? RemoveLastModifier()
        {
            if (str == null)
                return null;

            if (Rune.DecodeLastFromUtf16(str, out var _, out int markLength) == OperationStatus.Done)
            {
                string newStr = str[..^markLength];
                return string.IsNullOrEmpty(newStr) ? null : new Grapheme(newStr);
            }

            return null;
        }

        public override string ToString()
        {
            return str ?? c.ToString();
        }

        public bool Equals(Grapheme other)
        {
            return str == other.str && c == other.c;
        }

        public override bool Equals(object? obj)
        {
            return obj is Grapheme other && this == other;
        }

        /// <summary>
        /// Gets an enumerator that yields <see cref="Grapheme"/> instances from the given text.
        /// </summary>
        /// <param name="text">The text to enumerate graphemes from.</param>
        /// <remarks>
        /// This method correctly handles complex Unicode scenarios such as:
        /// - Emoji with skin tone modifiers (👋🏽)
        /// - Characters with combining marks (é = e + ´)
        /// - Regional indicator sequences (country flags)
        /// - Zero-width joiner sequences
        /// </remarks>
        public static IEnumerable<Grapheme> GetGraphemeEnumerator(string text)
        {
            if (string.IsNullOrEmpty(text))
                yield break;

            int index = 0;

            while (index < text.Length)
            {
                int length = StringInfo.GetNextTextElementLength(text, index);

                // If the text element is a single character, use the char constructor for efficiency
                if (length == 1)
                {
                    yield return new Grapheme(text[index]);
                }
                else
                {
                    // Otherwise use the string constructor for multi-character elements
                    yield return new Grapheme(text.Substring(index, length));
                }

                index += length;
            }
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(c, str);
        }
    }
}
