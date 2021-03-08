// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Localisation;

namespace osu.Framework.Tests.Localisation
{
    [TestFixture]
    public class LocalisableStringTest
    {
        private const string string_a = "a";
        private const string string_b = "b";

        [Test]
        public void TestTranslatableStringEqualsTranslatableString()
        {
            var str1 = new TranslatableString(string_a, string_b, string_a, string_b);
            var str2 = new TranslatableString(string_b, string_a, string_a, string_b);

            Assert.That(str1.Equals(str1));
            Assert.That(str1.Equals(new TranslatableString(string_a, string_b, string_a, string_b))); // Structurally equal
            Assert.That(!str1.Equals(str2));
        }

        [Test]
        public void TestRomanisableStringEqualsRomanisableString()
        {
            var str1 = new RomanisableString(string_a, string_b);
            var str2 = new RomanisableString(string_b, string_a);

            Assert.That(str1.Equals(str1));
            Assert.That(str1.Equals(new RomanisableString(string_a, string_b))); // Structurally equal
            Assert.That(!str1.Equals(str2));
        }
    }
}
