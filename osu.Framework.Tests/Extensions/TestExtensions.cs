// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using NUnit.Framework;
using osu.Framework.Extensions;

namespace osu.Framework.Tests.Extensions
{
    [TestFixture]
    public class TestExtensions
    {
        [TestCase(TestEnum.Value1, "Value1")]
        [TestCase(TestEnum.Value2, "V2")]
        [TestCase((TestEnum)3, "3")]
        public void TestGetDescription(TestEnum enumValue, string expected)
        {
            Assert.That(enumValue.GetDescription(), Is.EqualTo(expected));
        }

        public enum TestEnum
        {
            Value1,

            [System.ComponentModel.Description("V2")]
            Value2,
        }
    }
}
