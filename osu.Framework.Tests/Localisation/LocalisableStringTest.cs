// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using NUnit.Framework;
using osu.Framework.Localisation;

namespace osu.Framework.Tests.Localisation
{
    [TestFixture]
    public class LocalisableStringTest
    {
        private string makeStringA => makeString('a');
        private string makeStringB => makeString('b');

        private IFormattable makeFormattableA => new DateTime(1);
        private IFormattable makeFormattableB => new DateTime(2);

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
            var str1 = new LocalisableFormattableString(makeFormattableA, makeStringB);
            var str2 = new LocalisableFormattableString(makeFormattableB, makeStringA);

            testEquals(true, str1, str1);
            testEquals(true, str1, new LocalisableFormattableString(makeFormattableA, makeStringB));
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
            LocalisableString localisable = new LocalisableFormattableString(makeFormattableA, makeStringB);

            testEquals(true, localisable, new LocalisableFormattableString(makeFormattableA, makeStringB));
            testEquals(false, localisable, new LocalisableFormattableString(makeFormattableB, makeStringA));
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

        private static void testEquals<T>(bool expected, T a, T b)
        {
            var comparer = EqualityComparer<T>.Default;

            Assert.That(comparer.Equals(a, b), Is.EqualTo(expected));
            Assert.That(comparer.GetHashCode(a) == comparer.GetHashCode(b), Is.EqualTo(expected));
        }

        private static string makeString(params char[] chars) => new string(chars);
    }
}
