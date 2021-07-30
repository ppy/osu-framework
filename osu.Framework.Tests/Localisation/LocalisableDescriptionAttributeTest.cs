// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using NUnit.Framework;
using osu.Framework.Extensions;
using osu.Framework.Localisation;

namespace osu.Framework.Tests.Localisation
{
    [TestFixture]
    public class LocalisableDescriptionAttributeTest
    {
        [TestCase(EnumA.Item1, "Item1")]
        [TestCase(EnumA.Item2, "B")]
        public void TestNonLocalisableDescriptionReturnsDescriptionOrToString(EnumA value, string expected)
        {
            Assert.That(value.GetLocalisableDescription().ToString(), Is.EqualTo(expected));
        }

        [TestCase(EnumB.Item1, "Localised A")]
        [TestCase(EnumB.Item2, "Localised B")]
        public void TestLocalisableDescriptionReturnsDesignatedString(EnumB value, string expected)
        {
            Assert.That(value.GetLocalisableDescription().ToString(), Is.EqualTo(expected));
        }

        [Test]
        public void TestClassLocalisableDescription()
        {
            Assert.That(new ClassA().GetLocalisableDescription().ToString(), Is.EqualTo("Localised A"));
        }

        [Test]
        public void TestLocalisableDescriptionWithNonExistingMemberThrows()
        {
            Assert.Throws<InvalidOperationException>(() => EnumC.Item1.GetLocalisableDescription());
        }

        [Test]
        public void TestLocalisableDescriptionWithInstanceMemberThrows()
        {
            Assert.Throws<InvalidOperationException>(() => EnumD.Item1.GetLocalisableDescription());
        }

        public enum EnumA
        {
            Item1,

            [System.ComponentModel.Description("B")]
            Item2
        }

        public enum EnumB
        {
            [LocalisableDescription(typeof(TestStrings), nameof(TestStrings.A))]
            Item1,

            [LocalisableDescription(typeof(TestStrings), nameof(TestStrings.B))]
            Item2
        }

        public enum EnumC
        {
            [LocalisableDescription(typeof(LocalisableDescriptionAttributeTest), nameof(TestStrings.A))]
            Item1,
        }

        public enum EnumD
        {
            [LocalisableDescription(typeof(TestStrings), nameof(TestStrings.Instance))]
            Item1,
        }

        [LocalisableDescription(typeof(TestStrings), nameof(TestStrings.A))]
        public class ClassA
        {
        }

        private class TestStrings
        {
            public static LocalisableString A => "Localised A";

            public static readonly LocalisableString B = "Localised B";

            public LocalisableString Instance => string.Empty;
        }
    }
}
