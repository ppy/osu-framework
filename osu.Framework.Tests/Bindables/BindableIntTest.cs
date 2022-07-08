// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using NUnit.Framework;
using osu.Framework.Bindables;

namespace osu.Framework.Tests.Bindables
{
    [TestFixture]
    public class BindableIntTest
    {
        [TestCase(0)]
        [TestCase(-0)]
        [TestCase(1)]
        [TestCase(-105)]
        [TestCase(105)]
        [TestCase(int.MinValue)]
        [TestCase(int.MaxValue)]
        public void TestSet(int value)
        {
            var bindable = new BindableInt { Value = value };
            Assert.AreEqual(value, bindable.Value);
        }

        [TestCase("0", 0)]
        [TestCase("1", 1)]
        [TestCase("-0", 0)]
        [TestCase("-105", -105)]
        [TestCase("105", 105)]
        public void TestParsingString(string value, int expected)
        {
            var bindable = new BindableInt();
            bindable.Parse(value);

            Assert.AreEqual(expected, bindable.Value);
        }

        [TestCase("0", -10, 10, 0)]
        [TestCase("1", -10, 10, 1)]
        [TestCase("-0", -10, 10, 0)]
        [TestCase("-105", -10, 10, -10)]
        [TestCase("105", -10, 10, 10)]
        public void TestParsingStringWithRange(string value, int minValue, int maxValue, int expected)
        {
            var bindable = new BindableInt { MinValue = minValue, MaxValue = maxValue };
            bindable.Parse(value);

            Assert.AreEqual(expected, bindable.Value);
        }

        [TestCase(0)]
        [TestCase(-0)]
        [TestCase(1)]
        [TestCase(-105)]
        [TestCase(105)]
        [TestCase(int.MinValue)]
        [TestCase(int.MaxValue)]
        public void TestParsingInt(int value)
        {
            var bindable = new BindableInt();
            bindable.Parse(value);

            Assert.AreEqual(value, bindable.Value);
        }
    }
}
