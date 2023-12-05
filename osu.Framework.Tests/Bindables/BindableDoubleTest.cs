// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Globalization;
using NUnit.Framework;
using osu.Framework.Bindables;
using osu.Framework.Utils;

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

        [TestCase(-1.0, 1.0)]
        [TestCase(1.1, 1.0)]
        [TestCase(-1.1, 1.0)]
        [TestCase(-1.00000000, 1.00000000, 0.00000001)]
        [TestCase(1.00000001, 1.00000000, 0.00000001)]
        [TestCase(-1.00000001, 1.00000000, 0.00000001)]
        [TestCase(0.99999999, 1.00000000, 0.00000001)]
        [TestCase(-0.99999999, 1.00000000, 0.00000001)]
        [TestCase(199.1223345568, 199.1223345567, 0.0000000001)]
        [TestCase(-199.1223345568, 199.1223345567, 0.0000000001)]
        [TestCase(-199.1223345567, 199.1223345567, 0.0000000001)]
        public void TestDefaultCheck(double value, double def, double? precision = null)
        {
            var bindable = new BindableDouble { Value = def, Default = def };
            if (precision.HasValue)
                bindable.Precision = precision.Value;

            Assert.IsTrue(bindable.IsDefault);

            bindable.Value = value;
            Assert.IsFalse(bindable.IsDefault);
        }

        [TestCase("0", 0f)]
        [TestCase("1", 1f)]
        [TestCase("-0", 0f)]
        [TestCase("-105.123", -105.123)]
        [TestCase("105.123", 105.123)]
        public void TestParsingString(string value, double expected)
        {
            var bindable = new BindableDouble();
            bindable.Parse(value, CultureInfo.InvariantCulture);

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
            bindable.Parse(value, CultureInfo.InvariantCulture);

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
            bindable.Parse(value, CultureInfo.InvariantCulture);

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

        [TestCase("1.4", "en-US", 1.4)]
        [TestCase("1,4", "de-DE", 1.4)]
        [TestCase("1.400,01", "de-DE", 1400.01)]
        [TestCase("1 234,57", "ru-RU", 1234.57)]
        [TestCase("1,094", "fr-FR", 1.094)]
        [TestCase("1,400.01", "zh-CN", 1400.01)]
        public void TestParsingStringLocale(string value, string locale, double expected)
        {
            var bindable = new BindableDouble();
            bindable.Parse(value, CultureInfo.GetCultureInfo(locale));
            Assert.AreEqual(expected, bindable.Value);
        }

        [TestCase(1.4, "en-US", "1.4")]
        [TestCase(1.4, "de-DE", "1,4")]
        [TestCase(1400.01, "de-DE", "1400,01")]
        [TestCase(1234.57, "ru-RU", "1234,57")]
        [TestCase(1.094, "fr-FR", "1,094")]
        [TestCase(1400.01, "zh-CN", "1400.01")]
        public void TestParsingNumberLocale(double value, string locale, string expected)
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(locale);

            var bindable = new BindableDouble(value);
            string? asString = bindable.ToString();
            Assert.AreEqual(expected, asString);
            Assert.DoesNotThrow(() => bindable.Parse(asString, CultureInfo.CurrentCulture));
            Assert.AreEqual(value, bindable.Value, Precision.DOUBLE_EPSILON);
        }
    }
}
