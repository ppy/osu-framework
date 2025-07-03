// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using System.Text;
using NUnit.Framework;
using osu.Framework.Text;

namespace osu.Framework.Tests.Text
{
    [TestFixture]
    public class GraphemeTest
    {
        #region Constructor Tests

        /// <summary>
        /// Tests that the char constructor correctly creates a BMP grapheme.
        /// </summary>
        [Test]
        public void TestCharConstructor()
        {
            var grapheme = new Grapheme('a');

            Assert.That(grapheme.CharValue, Is.EqualTo('a'));
            Assert.That(grapheme.StringValue, Is.Null);
            Assert.That(grapheme.IsBmp, Is.True);
            Assert.That(grapheme.IsSurrogate, Is.False);
            Assert.That(grapheme.IsSingleScalarValue, Is.True);
            Assert.That(grapheme.Utf16SequenceLength, Is.EqualTo(1));
        }

        /// <summary>
        /// Tests that the string constructor correctly handles single BMP characters.
        /// </summary>
        [Test]
        public void TestStringConstructorWithSingleCharacter()
        {
            var grapheme = new Grapheme("a");

            Assert.That(grapheme.CharValue, Is.EqualTo('a'));
            Assert.That(grapheme.StringValue, Is.Null);
            Assert.That(grapheme.IsBmp, Is.True);
            Assert.That(grapheme.IsSurrogate, Is.False);
            Assert.That(grapheme.IsSingleScalarValue, Is.True);
            Assert.That(grapheme.Utf16SequenceLength, Is.EqualTo(1));
        }

        /// <summary>
        /// Tests that the string constructor correctly handles surrogate pairs.
        /// </summary>
        [Test]
        public void TestStringConstructorWithSurrogatePair()
        {
            var grapheme = new Grapheme("🙂"); // U+1F642 SLIGHTLY SMILING FACE

            Assert.That(grapheme.CharValue, Is.EqualTo('\uFFFD')); // replacement character for high surrogate
            Assert.That(grapheme.StringValue, Is.EqualTo("🙂"));
            Assert.That(grapheme.IsBmp, Is.False);
            Assert.That(grapheme.IsSurrogate, Is.True);
            Assert.That(grapheme.IsSingleScalarValue, Is.True);
            Assert.That(grapheme.Utf16SequenceLength, Is.EqualTo(2));
        }

        /// <summary>
        /// Tests that the string constructor correctly handles complex grapheme clusters.
        /// </summary>
        [Test]
        public void TestStringConstructorWithComplexGrapheme()
        {
            var grapheme = new Grapheme("☝🏽"); // Pointing up with medium skin tone

            Assert.That(grapheme.CharValue, Is.EqualTo('\u261D')); // first char of the sequence
            Assert.That(grapheme.StringValue, Is.EqualTo("☝🏽"));
            Assert.That(grapheme.IsBmp, Is.False);
            Assert.That(grapheme.IsSurrogate, Is.False);
            Assert.That(grapheme.IsSingleScalarValue, Is.False);
            Assert.That(grapheme.Utf16SequenceLength, Is.EqualTo(3));
        }

        /// <summary>
        /// Tests that the ReadOnlySpan constructor works correctly.
        /// </summary>
        [Test]
        public void TestReadOnlySpanConstructor()
        {
            var span = "🙂".AsSpan();
            var grapheme = new Grapheme(span);

            Assert.That(grapheme.CharValue, Is.EqualTo('\uFFFD'));
            Assert.That(grapheme.StringValue, Is.EqualTo("🙂"));
            Assert.That(grapheme.IsBmp, Is.False);
            Assert.That(grapheme.IsSurrogate, Is.True);
            Assert.That(grapheme.IsSingleScalarValue, Is.True);
            Assert.That(grapheme.Utf16SequenceLength, Is.EqualTo(2));
        }

        #endregion

        #region Property Tests

        /// <summary>
        /// Tests that IsBmp correctly identifies Basic Multilingual Plane characters.
        /// </summary>
        [Test]
        public void TestIsBmpProperty()
        {
            Assert.That(new Grapheme('a').IsBmp, Is.True);
            Assert.That(new Grapheme('€').IsBmp, Is.True);
            Assert.That(new Grapheme('\u2603').IsBmp, Is.True); // snowman
            Assert.That(new Grapheme("🙂").IsBmp, Is.False);
            Assert.That(new Grapheme("👋🏽").IsBmp, Is.False);
        }

        /// <summary>
        /// Tests that IsSurrogate correctly identifies surrogate pairs.
        /// </summary>
        [Test]
        public void TestIsSurrogateProperty()
        {
            Assert.That(new Grapheme('a').IsSurrogate, Is.False);
            Assert.That(new Grapheme("🙂").IsSurrogate, Is.True);
            Assert.That(new Grapheme("👋🏽").IsSurrogate, Is.False); // complex grapheme cluster, not a simple surrogate pair
        }

        /// <summary>
        /// Tests that IsSingleScalarValue correctly identifies single Unicode scalar values.
        /// </summary>
        [Test]
        public void TestIsSingleScalarValueProperty()
        {
            Assert.That(new Grapheme('a').IsSingleScalarValue, Is.True);
            Assert.That(new Grapheme("🙂").IsSingleScalarValue, Is.True);
            Assert.That(new Grapheme("👋🏽").IsSingleScalarValue, Is.False);
            Assert.That(new Grapheme("é").IsSingleScalarValue, Is.True); // precomposed
        }

        /// <summary>
        /// Tests that Utf16SequenceLength returns correct values.
        /// </summary>
        [Test]
        public void TestUtf16SequenceLengthProperty()
        {
            Assert.That(new Grapheme('a').Utf16SequenceLength, Is.EqualTo(1));
            Assert.That(new Grapheme("🙂").Utf16SequenceLength, Is.EqualTo(2));
            Assert.That(new Grapheme("👋🏽").Utf16SequenceLength, Is.EqualTo(4));
        }

        #endregion

        #region Method Tests

        /// <summary>
        /// Tests that IsWhiteSpace correctly identifies whitespace characters.
        /// </summary>
        [Test]
        public void TestIsWhiteSpaceMethod()
        {
            Assert.That(new Grapheme(' ').IsWhiteSpace(), Is.True);
            Assert.That(new Grapheme('\t').IsWhiteSpace(), Is.True);
            Assert.That(new Grapheme('\n').IsWhiteSpace(), Is.True);
            Assert.That(new Grapheme('\r').IsWhiteSpace(), Is.True);
            Assert.That(new Grapheme('a').IsWhiteSpace(), Is.False);
            Assert.That(new Grapheme("🙂").IsWhiteSpace(), Is.False);
        }

        /// <summary>
        /// Tests that RemoveLastModifier works correctly with emoji skin tones.
        /// </summary>
        [Test]
        public void TestRemoveLastModifierWithSkinTone()
        {
            var emojiWithSkinTone = new Grapheme("👋🏽");
            var baseEmoji = emojiWithSkinTone.RemoveLastModifier();

            Assert.That(baseEmoji, Is.Not.Null);
            Assert.That(baseEmoji?.ToString(), Is.EqualTo("👋"));
        }

        /// <summary>
        /// Tests that RemoveLastModifier returns null for BMP characters.
        /// </summary>
        [Test]
        public void TestRemoveLastModifierWithBmpCharacter()
        {
            var bmpChar = new Grapheme('a');
            var result = bmpChar.RemoveLastModifier();

            Assert.That(result, Is.Null);
        }

        /// <summary>
        /// Tests that RemoveLastModifier returns null for simple surrogate pairs.
        /// </summary>
        [Test]
        public void TestRemoveLastModifierWithSimpleSurrogatePair()
        {
            var emoji = new Grapheme("🙂");
            var result = emoji.RemoveLastModifier();

            Assert.That(result, Is.Null);
        }

        /// <summary>
        /// Tests that ToString returns correct string representation.
        /// </summary>
        [Test]
        public void TestToStringMethod()
        {
            Assert.That(new Grapheme('a').ToString(), Is.EqualTo("a"));
            Assert.That(new Grapheme("🙂").ToString(), Is.EqualTo("🙂"));
            Assert.That(new Grapheme("👋🏽").ToString(), Is.EqualTo("👋🏽"));
        }

        #endregion

        #region Equality Tests

        /// <summary>
        /// Tests that Equals works correctly for identical graphemes.
        /// </summary>
        [Test]
        public void TestEqualsWithIdenticalGraphemes()
        {
            var grapheme1 = new Grapheme('a');
            var grapheme2 = new Grapheme('a');

            Assert.That(grapheme1.Equals(grapheme2), Is.True);
            Assert.That(grapheme1 == grapheme2, Is.True);
            Assert.That(grapheme1 != grapheme2, Is.False);
        }

        /// <summary>
        /// Tests that Equals works correctly for different graphemes.
        /// </summary>
        [Test]
        public void TestEqualsWithDifferentGraphemes()
        {
            var grapheme1 = new Grapheme('a');
            var grapheme2 = new Grapheme('b');

            Assert.That(grapheme1.Equals(grapheme2), Is.False);
            Assert.That(grapheme1 == grapheme2, Is.False);
            Assert.That(grapheme1 != grapheme2, Is.True);
        }

        /// <summary>
        /// Tests that Equals works correctly for complex graphemes.
        /// </summary>
        [Test]
        public void TestEqualsWithComplexGraphemes()
        {
            var grapheme1 = new Grapheme("👋🏽");
            var grapheme2 = new Grapheme("👋🏽");
            var grapheme3 = new Grapheme("👋🏾");

            Assert.That(grapheme1.Equals(grapheme2), Is.True);
            Assert.That(grapheme1 == grapheme2, Is.True);
            Assert.That(grapheme1.Equals(grapheme3), Is.False);
            Assert.That(grapheme1 == grapheme3, Is.False);
        }

        /// <summary>
        /// Tests that GetHashCode produces consistent results.
        /// </summary>
        [Test]
        public void TestGetHashCode()
        {
            var grapheme1 = new Grapheme('a');
            var grapheme2 = new Grapheme('a');
            var grapheme3 = new Grapheme('b');

            Assert.That(grapheme1.GetHashCode(), Is.EqualTo(grapheme2.GetHashCode()));
            Assert.That(grapheme1.GetHashCode(), Is.Not.EqualTo(grapheme3.GetHashCode()));
        }

        #endregion

        #region Operator Tests

        /// <summary>
        /// Tests explicit conversion from char to Grapheme.
        /// </summary>
        [Test]
        public void TestExplicitCastFromChar()
        {
            char c = 'a';
            var grapheme = (Grapheme)c;

            Assert.That(grapheme.CharValue, Is.EqualTo('a'));
            Assert.That(grapheme.IsBmp, Is.True);
        }

        /// <summary>
        /// Tests explicit conversion from Rune to Grapheme.
        /// </summary>
        [Test]
        public void TestExplicitCastFromRune()
        {
            var rune = new Rune('a');
            var grapheme = (Grapheme)rune;

            Assert.That(grapheme.CharValue, Is.EqualTo('a'));
            Assert.That(grapheme.IsBmp, Is.True);
        }

        /// <summary>
        /// Tests explicit conversion from Grapheme to Rune.
        /// </summary>
        [Test]
        public void TestExplicitCastToRune()
        {
            var grapheme = new Grapheme('a');
            var rune = (Rune)grapheme;

            Assert.That(rune.Value, Is.EqualTo('a'));
        }

        /// <summary>
        /// Tests explicit conversion from surrogate pair Grapheme to Rune.
        /// </summary>
        [Test]
        public void TestExplicitCastSurrogatePairToRune()
        {
            var grapheme = new Grapheme("🙂");
            var rune = (Rune)grapheme;

            Assert.That(rune.ToString(), Is.EqualTo("🙂"));
        }

        #endregion

        #region Static Method Tests

        /// <summary>
        /// Tests that GetGraphemeEnumerator correctly handles simple text.
        /// </summary>
        [Test]
        public void TestGetGraphemeEnumeratorWithSimpleText()
        {
            var text = "Hello";
            var graphemes = Grapheme.GetGraphemeEnumerator(text).ToArray();

            Assert.That(graphemes.Length, Is.EqualTo(5));
            Assert.That(graphemes[0].CharValue, Is.EqualTo('H'));
            Assert.That(graphemes[1].CharValue, Is.EqualTo('e'));
            Assert.That(graphemes[2].CharValue, Is.EqualTo('l'));
            Assert.That(graphemes[3].CharValue, Is.EqualTo('l'));
            Assert.That(graphemes[4].CharValue, Is.EqualTo('o'));
        }

        /// <summary>
        /// Tests that GetGraphemeEnumerator correctly handles complex Unicode text.
        /// </summary>
        [Test]
        public void TestGetGraphemeEnumeratorWithComplexText()
        {
            var text = "Hello 👋🏽 World!";
            var graphemes = Grapheme.GetGraphemeEnumerator(text).ToArray();

            Assert.That(graphemes.Length, Is.EqualTo(14));
            Assert.That(graphemes[6].ToString(), Is.EqualTo("👋🏽"));
            Assert.That(graphemes[6].IsSingleScalarValue, Is.False);
            Assert.That(graphemes[6].Utf16SequenceLength, Is.EqualTo(4));
        }

        /// <summary>
        /// Tests that GetGraphemeEnumerator returns empty enumerable for null or empty strings.
        /// </summary>
        [Test]
        public void TestGetGraphemeEnumeratorWithEmptyText()
        {
            var graphemes1 = Grapheme.GetGraphemeEnumerator(null!).ToArray();
            var graphemes2 = Grapheme.GetGraphemeEnumerator("").ToArray();

            Assert.That(graphemes1.Length, Is.EqualTo(0));
            Assert.That(graphemes2.Length, Is.EqualTo(0));
        }

        /// <summary>
        /// Tests that GetGraphemeEnumerator correctly handles text with combining characters.
        /// </summary>
        [Test]
        public void TestGetGraphemeEnumeratorWithCombiningCharacters()
        {
            // "café" with combining acute accent (e + ´)
            string text = "cafe\u0301";
            var graphemes = Grapheme.GetGraphemeEnumerator(text).ToArray();

            Assert.That(graphemes.Length, Is.EqualTo(4));
            Assert.That(graphemes[3].ToString(), Is.EqualTo("é"));
            Assert.That(graphemes[3].Utf16SequenceLength, Is.EqualTo(2));
        }

        #endregion

        #region Edge Cases and Special Characters

        /// <summary>
        /// Tests handling of null character.
        /// </summary>
        [Test]
        public void TestNullCharacter()
        {
            var grapheme = new Grapheme('\0');

            Assert.That(grapheme.CharValue, Is.EqualTo('\0'));
            Assert.That(grapheme.IsBmp, Is.True);
            Assert.That(grapheme.ToString(), Is.EqualTo("\0"));
        }

        /// <summary>
        /// Tests handling of replacement character.
        /// </summary>
        [Test]
        public void TestReplacementCharacter()
        {
            var grapheme = new Grapheme('\uFFFD');

            Assert.That(grapheme.CharValue, Is.EqualTo('\uFFFD'));
            Assert.That(grapheme.IsBmp, Is.True);
            Assert.That(grapheme.ToString(), Is.EqualTo("�"));
        }

        /// <summary>
        /// Tests that graphemes preserve original string exactly.
        /// </summary>
        [Test]
        public void TestStringPreservation()
        {
            var originalString = "👋🏽";
            var grapheme = new Grapheme(originalString);

            Assert.That(grapheme.ToString(), Is.EqualTo(originalString));
            Assert.That(grapheme.StringValue, Is.EqualTo(originalString));
        }

        /// <summary>
        /// Tests handling of regional indicator sequences (flags).
        /// </summary>
        [Test]
        public void TestRegionalIndicatorSequence()
        {
            // US flag (🇺🇸)
            var flag = "🇺🇸";
            var grapheme = new Grapheme(flag);

            Assert.That(grapheme.ToString(), Is.EqualTo(flag));
            Assert.That(grapheme.IsBmp, Is.False);
            Assert.That(grapheme.IsSingleScalarValue, Is.False);
            Assert.That(grapheme.Utf16SequenceLength, Is.EqualTo(4));
        }

        #endregion
    }
}
