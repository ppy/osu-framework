// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using NUnit.Framework;
using osu.Framework.Configuration;

namespace osu.Framework.Tests.Bindables
{
    [TestFixture]
    public class BindableEnumTest
    {
        [TestCase(TestEnum.Value1)]
        [TestCase(TestEnum.Value2)]
        [TestCase(TestEnum.Value1 - 1)]
        [TestCase(TestEnum.Value2 + 1)]
        public void TestSet(TestEnum value)
        {
            var bindable = new Bindable<TestEnum> { Value = value };
            Assert.AreEqual(value, bindable.Value);
        }

        [TestCase("Value1", TestEnum.Value1)]
        [TestCase("Value2", TestEnum.Value2)]
        [TestCase("-1", TestEnum.Value1 - 1)]
        [TestCase("2", TestEnum.Value2 + 1)]
        public void TestParsingString(string value, TestEnum expected)
        {
            var bindable = new Bindable<TestEnum>();
            bindable.Parse(value);

            Assert.AreEqual(expected, bindable.Value);
        }

        [TestCase(TestEnum.Value1)]
        [TestCase(TestEnum.Value2)]
        [TestCase(TestEnum.Value1 - 1)]
        [TestCase(TestEnum.Value2 + 1)]
        public void TestParsingEnum(TestEnum value)
        {
            var bindable = new Bindable<TestEnum>();
            bindable.Parse(value);

            Assert.AreEqual(value, bindable.Value);
        }

        public enum TestEnum
        {
            Value1 = 0,
            Value2 = 1
        }
    }
}
