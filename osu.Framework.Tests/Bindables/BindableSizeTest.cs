// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using NUnit.Framework;
using System.Drawing;
using osu.Framework.Bindables;

namespace osu.Framework.Tests.Bindables
{
    [TestFixture]
    public class BindableSizeTest
    {
        [TestCase(0, 0)]
        [TestCase(1, 0)]
        [TestCase(1, -1)]
        [TestCase(int.MinValue, int.MinValue)]
        [TestCase(int.MaxValue, int.MaxValue)]
        [TestCase(int.MinValue, 0)]
        public void TestSet(int width, int height)
        {
            var value = new Size(width, height);

            var bindable = new BindableSize { Value = value };
            Assert.AreEqual(value, bindable.Value);
        }

        [TestCase("0x0", 0, 0)]
        [TestCase("-0x-0", 0, 0)]
        [TestCase("1920x1080", 1920, 1080)]
        [TestCase("-1000x0", -1000, 0)]
        [TestCase("0x-1000", 0, -1000)]
        public void TestParsingString(string value, int expectedWidth, int expectedHeight)
        {
            var bindable = new BindableSize();
            bindable.Parse(value);

            Assert.AreEqual(new Size(expectedWidth, expectedHeight), bindable.Value);
        }

        [TestCase("0x0", -10, -10, 10, 10, 0, 0)]
        [TestCase("5x-5", -10, -10, 10, 10, 5, -5)]
        [TestCase("0x-100", 0, -10, 0, 10, 0, -10)]
        [TestCase("120x0", -10, -10, 10, 10, 10, 0)]
        [TestCase("-100x400", -25, 200, 25, 300, -25, 300)]
        public void TestParsingStringWithRange(string value,
                                               int minValueWidth, int minValueHeight,
                                               int maxValueWidth, int maxValueHeight,
                                               int expectedWidth, int expectedHeight
        )
        {
            var minValue = new Size(minValueWidth, minValueHeight);
            var maxValue = new Size(maxValueWidth, maxValueHeight);
            var expected = new Size(expectedWidth, expectedHeight);

            var bindable = new BindableSize { MinValue = minValue, MaxValue = maxValue };
            bindable.Parse(value);

            Assert.AreEqual(expected, bindable.Value);
        }

        [TestCase(0, 0)]
        [TestCase(1, 0)]
        [TestCase(1, -1)]
        [TestCase(int.MaxValue, int.MinValue)]
        public void TestParsingSize(int width, int height)
        {
            var value = new Size(width, height);

            var bindable = new BindableSize();
            bindable.Parse(value);

            Assert.AreEqual(value, bindable.Value);
        }
    }
}
