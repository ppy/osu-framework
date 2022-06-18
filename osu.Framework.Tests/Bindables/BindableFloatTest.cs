// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using NUnit.Framework;
using osu.Framework.Bindables;

namespace osu.Framework.Tests.Bindables
{
    [TestFixture]
    public class BindableFloatTest
    {
        [TestCase(0)]
        [TestCase(-0)]
        [TestCase(1)]
        [TestCase(-105.123f)]
        [TestCase(105.123f)]
        [TestCase(float.MinValue)]
        [TestCase(float.MaxValue)]
        public void TestSet(float value)
        {
            var bindable = new BindableFloat { Value = value };
            Assert.AreEqual(value, bindable.Value);
        }

        [TestCase(-1.0f, 1.0f)]
        [TestCase(1.1f, 1.0f)]
        [TestCase(-1.1f, 1.0f)]
        [TestCase(-1.00f, 1.00f, 0.01f)]
        [TestCase(1.01f, 1.00f, 0.01f)]
        [TestCase(-1.01f, 1.00f, 0.01f)]
        [TestCase(0.99f, 1.00f, 0.01f)]
        [TestCase(-0.99f, 1.00f, 0.01f)]
        [TestCase(105.123f, 105.122f, 0.001f)]
        [TestCase(-105.123f, 105.122f, 0.001f)]
        [TestCase(-105.122f, 105.122f, 0.001f)]
        public void TestDefaultCheck(float value, float def, float? precision = null)
        {
            var bindable = new BindableFloat { Value = def, Default = def };
            if (precision.HasValue)
                bindable.Precision = precision.Value;

            Assert.IsTrue(bindable.IsDefault);

            bindable.Value = value;
            Assert.IsFalse(bindable.IsDefault);
        }

        [TestCase("0", 0f)]
        [TestCase("1", 1f)]
        [TestCase("-0", 0f)]
        [TestCase("-105.123", -105.123f)]
        [TestCase("105.123", 105.123f)]
        public void TestParsingString(string value, float expected)
        {
            var bindable = new BindableFloat();
            bindable.Parse(value);

            Assert.AreEqual(expected, bindable.Value);
        }

        [TestCase("0", -10, 10, 0)]
        [TestCase("1", -10, 10, 1)]
        [TestCase("-0", -10, 10, 0)]
        [TestCase("-105.123", -10, 10, -10)]
        [TestCase("105.123", -10, 10, 10)]
        public void TestParsingStringWithRange(string value, float minValue, float maxValue, float expected)
        {
            var bindable = new BindableFloat { MinValue = minValue, MaxValue = maxValue };
            bindable.Parse(value);

            Assert.AreEqual(expected, bindable.Value);
        }

        [TestCase(0)]
        [TestCase(-0)]
        [TestCase(1)]
        [TestCase(-105.123f)]
        [TestCase(105.123f)]
        [TestCase(float.MinValue)]
        [TestCase(float.MaxValue)]
        public void TestParsingFloat(float value)
        {
            var bindable = new BindableFloat();
            bindable.Parse(value);

            Assert.AreEqual(value, bindable.Value);
        }
    }
}
