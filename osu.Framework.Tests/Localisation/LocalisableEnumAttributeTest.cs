// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using NUnit.Framework;
using osu.Framework.Extensions;
using osu.Framework.Localisation;

namespace osu.Framework.Tests.Localisation
{
    [TestFixture]
    public class LocalisableEnumAttributeTest
    {
        [TestCase(EnumA.Item1, "Item1")]
        [TestCase(EnumA.Item2, "B")]
        public void TestNonLocalisableEnumReturnsDescriptionOrToString(EnumA value, string expected)
        {
            Assert.That(value.GetLocalisableDescription().ToString(), Is.EqualTo(expected));
        }

        [TestCase(EnumB.Item1, "A")]
        [TestCase(EnumB.Item2, "B")]
        public void TestLocalisableEnumReturnsMappedValue(EnumB value, string expected)
        {
            Assert.That(value.GetLocalisableDescription().ToString(), Is.EqualTo(expected));
        }

        [Test]
        public void TestLocalisableEnumWithInvalidBaseTypeThrows()
        {
            // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
            Assert.Throws<ArgumentException>(() => EnumC.Item1.GetLocalisableDescription().ToString());
        }

        [Test]
        public void TestLocalisableEnumWithInvalidGenericTypeThrows()
        {
            // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
            Assert.Throws<InvalidOperationException>(() => EnumD.Item1.GetLocalisableDescription().ToString());
        }

        public enum EnumA
        {
            Item1,

            [System.ComponentModel.Description("B")]
            Item2
        }

        [LocalisableEnum(typeof(EnumBEnumLocalisationMapper))]
        public enum EnumB
        {
            Item1,
            Item2
        }

        private class EnumBEnumLocalisationMapper : EnumLocalisationMapper<EnumB>
        {
            public override LocalisableString Map(EnumB value)
            {
                switch (value)
                {
                    case EnumB.Item1:
                        return "A";

                    case EnumB.Item2:
                        return "B";

                    default:
                        throw new ArgumentOutOfRangeException(nameof(value), value, null);
                }
            }
        }

        [LocalisableEnum(typeof(EnumCEnumLocalisationMapper))]
        public enum EnumC
        {
            Item1,
        }

        private class EnumCEnumLocalisationMapper
        {
        }

        [LocalisableEnum(typeof(EnumDEnumLocalisationMapper))]
        public enum EnumD
        {
            Item1,
        }

        private class EnumDEnumLocalisationMapper : EnumLocalisationMapper<EnumA>
        {
            public override LocalisableString Map(EnumA value) => "A";
        }
    }
}
