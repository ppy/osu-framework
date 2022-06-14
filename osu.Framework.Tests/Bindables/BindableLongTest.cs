// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using NUnit.Framework;
using osu.Framework.Bindables;

namespace osu.Framework.Tests.Bindables
{
    [TestFixture]
    public class BindableLongTest
    {
        [TestCase(0)]
        [TestCase(-0)]
        [TestCase(1)]
        [TestCase(-105)]
        [TestCase(105)]
        [TestCase(long.MinValue)]
        [TestCase(long.MaxValue)]
        public void TestSet(long value)
        {
            var bindable = new BindableLong { Value = value };
            Assert.AreEqual(value, bindable.Value);
        }

        [TestCase("0", 0)]
        [TestCase("1", 1)]
        [TestCase("-0", 0)]
        [TestCase("-105", -105)]
        [TestCase("105", 105)]
        public void TestParsingString(string value, long expected)
        {
            var bindable = new BindableLong();
            bindable.Parse(value);

            Assert.AreEqual(expected, bindable.Value);
        }

        [TestCase("0", -10, 10, 0)]
        [TestCase("1", -10, 10, 1)]
        [TestCase("-0", -10, 10, 0)]
        [TestCase("-105", -10, 10, -10)]
        [TestCase("105", -10, 10, 10)]
        public void TestParsingStringWithRange(string value, long minValue, long maxValue, long expected)
        {
            var bindable = new BindableLong { MinValue = minValue, MaxValue = maxValue };
            bindable.Parse(value);

            Assert.AreEqual(expected, bindable.Value);
        }

        [TestCase(0)]
        [TestCase(-0)]
        [TestCase(1)]
        [TestCase(-105)]
        [TestCase(105)]
        [TestCase(long.MinValue)]
        [TestCase(long.MaxValue)]
        public void TestParsingLong(long value)
        {
            var bindable = new BindableLong();
            bindable.Parse(value);

            Assert.AreEqual(value, bindable.Value);
        }
    }
}
