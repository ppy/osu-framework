// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Configuration;

namespace osu.Framework.Tests.Bindables
{
    [TestFixture]
    public class BindableDoubleTest
    {
        [TestCase(0)]
        [TestCase(-0)]
        [TestCase(1)]
        [TestCase(-105.123)]
        [TestCase(105.123)]
        [TestCase(double.MinValue)]
        [TestCase(double.MaxValue)]
        public void TestSet(double value)
        {
            var bindable = new BindableDouble { Value = value };
            Assert.AreEqual(value, bindable.Value);
        }

        [TestCase("0", 0f)]
        [TestCase("1", 1f)]
        [TestCase("-0", 0f)]
        [TestCase("-105.123", -105.123)]
        [TestCase("105.123", 105.123)]
        public void TestParsingString(string value, double expected)
        {
            var bindable = new BindableDouble();
            bindable.Parse(value);

            Assert.AreEqual(expected, bindable.Value);
        }

        [TestCase("0", -10, 10, 0)]
        [TestCase("1", -10, 10, 1)]
        [TestCase("-0", -10, 10, 0)]
        [TestCase("-105.123", -10, 10, -10)]
        [TestCase("105.123", -10, 10, 10)]
        public void TestParsingStringWithRange(string value, double minValue, double maxValue, double expected)
        {
            var bindable = new BindableDouble { MinValue = minValue, MaxValue = maxValue };
            bindable.Parse(value);

            Assert.AreEqual(expected, bindable.Value);
        }

        [TestCase(0)]
        [TestCase(-0)]
        [TestCase(1)]
        [TestCase(-105.123)]
        [TestCase(105.123)]
        [TestCase(double.MinValue)]
        [TestCase(double.MaxValue)]
        public void TestParsingDouble(double value)
        {
            var bindable = new BindableDouble();
            bindable.Parse(value);

            Assert.AreEqual(value, bindable.Value);
        }

        [Test]
        public void TestPropagationToPlainBindable()
        {
            var number = new BindableDouble(1000);
            var bindable = new Bindable<double>();

            bindable.BindTo(number);

            number.Precision = 0.5f;
            number.MinValue = 0;
            number.MaxValue = 10;
        }
    }
}
