// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using NUnit.Framework;
using osu.Framework.Localisation;

namespace osu.Framework.Tests.Localisation
{
    [TestFixture]
    public class LocalisableStringTest
    {
        private string makeStringA => makeString('a');
        private string makeStringB => makeString('b');

        [Test]
        public void TestTranslatableStringEqualsTranslatableString()
        {
            var str1 = new TranslatableString(makeStringA, makeStringB, makeStringA, makeStringB);
            var str2 = new TranslatableString(makeStringA, makeStringB);

            testEquals(true, str1, str1);
            testEquals(true, str1, new TranslatableString(makeStringA, makeStringB, makeStringA, makeStringB)); // Structural equality.
            testEquals(false, str1, str2);
        }

        [Test]
        public void TestRomanisableStringEqualsRomanisableString()
        {
            var str1 = new RomanisableString(makeStringA, makeStringB);
            var str2 = new RomanisableString(makeStringB, makeStringA);

            testEquals(true, str1, str1);
            testEquals(true, str1, new RomanisableString(makeStringA, makeStringB)); // Structural equality.
            testEquals(false, str1, str2);
        }

        [Test]
        public void TestLocalisableFormattableEqualsLocalisableFormattable()
        {
            var str1 = LocalisableString.Format(makeStringA, makeStringB);
            var str2 = LocalisableString.Format(makeStringB, makeStringA);

            testEquals(true, str1, str1);
            testEquals(true, str1, LocalisableString.Format(makeStringA, makeStringB));
            testEquals(false, str1, str2);
        }

        [Test]
        public void TestLocalisableStringEqualsString()
        {
            LocalisableString localisable = makeStringA;

            testEquals(true, localisable, makeStringA);
            testEquals(false, localisable, makeStringB);
        }

        [Test]
        public void TestLocalisableStringEqualsTranslatableString()
        {
            LocalisableString localisable = new TranslatableString(makeStringA, makeStringB, makeStringA, makeStringB);

            testEquals(true, localisable, new TranslatableString(makeStringA, makeStringB, makeStringA, makeStringB));
            testEquals(false, localisable, new TranslatableString(makeStringB, makeStringA));
            testEquals(false, localisable, makeStringA);
            testEquals(false, localisable, new RomanisableString(makeStringA, makeStringB));
        }

        [Test]
        public void TestLocalisableStringEqualsRomanisableString()
        {
            LocalisableString localisable = new RomanisableString(makeStringA, makeStringB);

            testEquals(true, localisable, new RomanisableString(makeStringA, makeStringB));
            testEquals(false, localisable, new RomanisableString(makeStringB, makeStringA));
            testEquals(false, localisable, makeStringA);
            testEquals(false, localisable, new TranslatableString(makeStringA, makeStringB));
        }

        [Test]
        public void TestLocalisableStringEqualsLocalisableFormattable()
        {
            LocalisableString localisable = LocalisableString.Format(makeStringA, makeStringB);

            testEquals(true, localisable, LocalisableString.Format(makeStringA, makeStringB));
            testEquals(false, localisable, LocalisableString.Format(makeStringB, makeStringA));
        }

        [Test]
        public void TestNullEqualsNull()
        {
            testEquals(true, new LocalisableString(), new LocalisableString());
        }

        [Test]
        public void TestLocalisableStringDoesNotEqualNull()
        {
            testEquals(false, new LocalisableString(), new RomanisableString(makeStringA, makeStringB));
        }

        [Test]
        [SuppressMessage("ReSharper", "ConditionIsAlwaysTrueOrFalse")]
        public void TestIsNullOrEmpty()
        {
            Assert.That(LocalisableString.IsNullOrEmpty(null), Is.EqualTo(string.IsNullOrEmpty(null)));
            Assert.That(LocalisableString.IsNullOrEmpty(string.Empty), Is.EqualTo(string.IsNullOrEmpty(string.Empty)));
            Assert.That(LocalisableString.IsNullOrEmpty(""), Is.EqualTo(string.IsNullOrEmpty("")));
            Assert.That(LocalisableString.IsNullOrEmpty(" "), Is.EqualTo(string.IsNullOrEmpty(" ")));
            Assert.That(LocalisableString.IsNullOrEmpty("a"), Is.EqualTo(string.IsNullOrEmpty("a")));

            Assert.IsTrue(LocalisableString.IsNullOrEmpty(new LocalisableString())); // default(LocalisableString)
            Assert.IsFalse(LocalisableString.IsNullOrEmpty(new TranslatableString("key", "fallback")));
        }

        [Test]
        [SuppressMessage("ReSharper", "ConditionIsAlwaysTrueOrFalse")]
        public void TestIsNullOrWhiteSpace()
        {
            Assert.That(LocalisableString.IsNullOrWhiteSpace(null), Is.EqualTo(string.IsNullOrWhiteSpace(null)));
            Assert.That(LocalisableString.IsNullOrWhiteSpace(string.Empty), Is.EqualTo(string.IsNullOrWhiteSpace(string.Empty)));
            Assert.That(LocalisableString.IsNullOrWhiteSpace(""), Is.EqualTo(string.IsNullOrWhiteSpace("")));
            Assert.That(LocalisableString.IsNullOrWhiteSpace(" "), Is.EqualTo(string.IsNullOrWhiteSpace(" ")));
            Assert.That(LocalisableString.IsNullOrWhiteSpace("a"), Is.EqualTo(string.IsNullOrWhiteSpace("a")));

            Assert.IsTrue(LocalisableString.IsNullOrWhiteSpace(new LocalisableString())); // default(LocalisableString)
            Assert.IsFalse(LocalisableString.IsNullOrWhiteSpace(new TranslatableString("key", "fallback")));
        }

        private static void testEquals<T>(bool expected, T a, T b)
        {
            var comparer = EqualityComparer<T>.Default;

            Assert.That(comparer.Equals(a, b), Is.EqualTo(expected));
            Assert.That(comparer.GetHashCode(a) == comparer.GetHashCode(b), Is.EqualTo(expected));
        }

        private static string makeString(params char[] chars) => new string(chars);
    }
}
