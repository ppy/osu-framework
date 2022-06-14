// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using NUnit.Framework;
using osu.Framework.Bindables;
using osu.Framework.Graphics;

namespace osu.Framework.Tests.Bindables
{
    [TestFixture]
    public class BindableMarginPaddingTest
    {
        [TestCase(0, 0, 0, 0)]
        [TestCase(0, 1, 2, 3)]
        [TestCase(1, -1, 2, -2)]
        [TestCase(float.MinValue, float.MinValue, float.MinValue, float.MinValue)]
        [TestCase(float.MaxValue, float.MaxValue, float.MaxValue, float.MaxValue)]
        [TestCase(float.MinValue, 0, 0, 0)]
        public void TestSet(float top, float left, float bottom, float right)
        {
            var value = new MarginPadding { Top = top, Left = left, Bottom = bottom, Right = right };

            var bindable = new BindableMarginPadding { Value = value };
            Assert.AreEqual(value, bindable.Value);
        }

        [TestCase("(0, 0, 0, 0)", 0, 0, 0, 0)]
        [TestCase("(-0, -0, -0, -0)", 0, 0, 0, 0)]
        [TestCase("(1, 2, 3, 4)", 1, 2, 3, 4)]
        [TestCase("(1.5, 2.5, 3.5, 4.5)", 1.5f, 2.5f, 3.5f, 4.5f)]
        public void TestParsingString(string value, float expectedTop, float expectedLeft, float expectedBottom, float expectedRight)
        {
            var bindable = new BindableMarginPadding();
            bindable.Parse(value);

            Assert.AreEqual(new MarginPadding { Top = expectedTop, Left = expectedLeft, Bottom = expectedBottom, Right = expectedRight }, bindable.Value);
        }

        [TestCase("(0, 0, 0, 0)", -10, -10, -10, -10, 10, 10, 10, 10, 0, 0, 0, 0)]
        [TestCase("(5, -5, 5, -5)", -10, -10, -10, -10, 10, 10, 10, 10, 5, -5, 5, -5)]
        [TestCase("(-100, -100, -100, -100)", -10, -10, -10, -10, 10, 10, 10, 10, -10, -10, -10, -10)]
        [TestCase("(100, 100, 100, 100)", -10, -10, -10, -10, 10, 10, 10, 10, 10, 10, 10, 10)]
        public void TestParsingStringWithRange(string value,
                                               float minValueTop, float minValueLeft, float minValueBottom, float minValueRight,
                                               float maxValueTop, float maxValueLeft, float maxValueBottom, float maxValueRight,
                                               float expectedTop, float expectedLeft, float expectedBottom, float expectedRight
        )
        {
            var minValue = new MarginPadding { Top = minValueTop, Left = minValueLeft, Bottom = minValueBottom, Right = minValueRight };
            var maxValue = new MarginPadding { Top = maxValueTop, Left = maxValueLeft, Bottom = maxValueBottom, Right = maxValueRight };
            var expected = new MarginPadding { Top = expectedTop, Left = expectedLeft, Bottom = expectedBottom, Right = expectedRight };

            var bindable = new BindableMarginPadding { MinValue = minValue, MaxValue = maxValue };
            bindable.Parse(value);

            Assert.AreEqual(expected, bindable.Value);
        }
    }
}
