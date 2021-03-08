// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

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

            Assert.That(str1.Equals(str1));
            Assert.That(str1.Equals(new TranslatableString(makeStringA, makeStringB, makeStringA, makeStringB))); // Structurally equal
            Assert.That(!str1.Equals(str2));
        }

        [Test]
        public void TestRomanisableStringEqualsRomanisableString()
        {
            var str1 = new RomanisableString(makeStringA, makeStringB);
            var str2 = new RomanisableString(makeStringB, makeStringA);

            Assert.That(str1.Equals(str1));
            Assert.That(str1.Equals(new RomanisableString(makeStringA, makeStringB))); // Structurally equal
            Assert.That(!str1.Equals(str2));
        }

        [Test]
        public void TestLocalisableStringEqualsString()
        {
            LocalisableString localisable = makeStringA;

            Assert.That(localisable.Equals(makeStringA));
            Assert.That(!localisable.Equals(makeStringB));
        }

        [Test]
        public void TestLocalisableStringEqualsTranslatableString()
        {
            LocalisableString localisable = new TranslatableString(makeStringA, makeStringB, makeStringA, makeStringB);

            Assert.That(localisable.Equals(new TranslatableString(makeStringA, makeStringB, makeStringA, makeStringB))); // Structurally equal
            Assert.That(!localisable.Equals(new TranslatableString(makeStringB, makeStringA)));
            Assert.That(!localisable.Equals(makeStringA));
            Assert.That(!localisable.Equals(new RomanisableString(makeStringA, makeStringB)));
        }

        [Test]
        public void TestLocalisableStringEqualsRomanisableString()
        {
            LocalisableString localisable = new RomanisableString(makeStringA, makeStringB);

            Assert.That(localisable.Equals(new RomanisableString(makeStringA, makeStringB))); // Structurally equal
            Assert.That(!localisable.Equals(new RomanisableString(makeStringB, makeStringA)));
            Assert.That(!localisable.Equals(makeStringA));
            Assert.That(!localisable.Equals(new TranslatableString(makeStringA, makeStringB)));
        }

        private static string makeString(params char[] chars) => new string(chars);
    }
}
