// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using NUnit.Framework;
using osu.Framework.Localisation;

namespace osu.Framework.Tests.Localisation
{
    [TestFixture]
    public class LocalisableStringTest
    {
        private const string first_string = "abc123";
        private const string second_string = "123abc";

        [Test]
        public void TestNUnitEqualTo()
        {
            LocalisableString localisable = first_string;

            Assert.That(localisable, Is.EqualTo(first_string));
            Assert.That(localisable, Is.Not.EqualTo(second_string));
        }

        [Test]
        public void TestObjectEqualsTo()
        {
            LocalisableString localisable = first_string;

#pragma warning disable RS0030
            Assert.That(Equals(localisable, first_string));
            Assert.That(!Equals(localisable, second_string));
#pragma warning restore RS0030
        }

        [Test]
        public void TestOperatorEqualsString()
        {
            LocalisableString localisable = first_string;

            Assert.That(localisable == first_string);
            Assert.That(localisable != second_string);
        }

        [Test]
        public void TestEqualityStringComparerEquals()
        {
            LocalisableString localisable = first_string;

            Assert.That(EqualityComparer<string>.Default.Equals(localisable, first_string));
            Assert.That(!EqualityComparer<string>.Default.Equals(localisable, second_string));
        }

        [Test]
        public void TestEqualityLocalisableComparerEquals()
        {
            LocalisableString localisable = first_string;

            Assert.That(EqualityComparer<LocalisableString>.Default.Equals(localisable, first_string));
            Assert.That(!EqualityComparer<LocalisableString>.Default.Equals(localisable, second_string));
        }
    }
}
