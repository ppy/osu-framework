// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using NUnit.Framework;
using osu.Framework.Bindables;
using osuTK;

namespace osu.Framework.Tests.Bindables
{
    [TestFixture]
    public class BindableVector2DTest
    {
        [TestCase(0, 0)]
        [TestCase(1, 0)]
        [TestCase(1, -1)]
        [TestCase(0.1, 0)]
        [TestCase(-105.123, 105.123)]
        [TestCase(105.123, -105.123)]
        [TestCase(double.MinValue, double.MinValue)]
        [TestCase(double.MaxValue, double.MaxValue)]
        [TestCase(double.MinValue, 0)]
        public void TestSet(double width, double height)
        {
            var value = new Vector2d(width, height);

            var bindable = new BindableVector2D { Value = value };
            Assert.AreEqual(value, bindable.Value);
        }

        [TestCase("(0,0)", 0f, 0f)]
        [TestCase("(1,0)", 1f, 0f)]
        [TestCase("(-0,0)", 0f, 0f)]
        [TestCase("(-1000,0)", -1000f, 0f)]
        [TestCase("(0,-1000)", 0f, -1000f)]
        [TestCase("(-105.123,105.123)", -105.123, 105.123)]
        [TestCase("(105.123,105.123)", 105.123, -105.123)]
        public void TestParsingString(string value, double expectedWidth, double expectedHeight)
        {
            var bindable = new BindableVector2D();
            bindable.Parse(value);

            Assert.AreEqual(new Vector2d(expectedWidth, expectedHeight), bindable.Value);
        }

        [TestCase(0, 0)]
        [TestCase(1, 0)]
        [TestCase(1, -1)]
        [TestCase(double.MaxValue, double.MinValue)]
        public void TestParsingVector2D(double width, double height)
        {
            var value = new Vector2d(width, height);

            var bindable = new BindableVector2D();
            bindable.Parse(value);

            Assert.AreEqual(value, bindable.Value);
        }

        [TestCase("(0,0)", -10, 10, -10, 10, 0, 0)]
        [TestCase("(1,0)", -10, 10, -10, 10, 1, 0)]
        [TestCase("(0,1)", -10, 10, -10, 10, 0, 1)]
        [TestCase("(-0,-0)", -10, 10, -10, 10, 0, 0)]
        [TestCase("(-105.123,0)", -10, 10, -10, 10, -10, 0)]
        [TestCase("(105.123,0)", -10, 10, -10, 10, 10, 0)]
        [TestCase("(0,-105.123)", -10, 10, -10, 10, 0, -10)]
        [TestCase("(0,105.123)", -10, 10, -10, 10, 0, 10)]
        public void TestParsingStringWithRange(string value,
                                               double minValueX, double minValueY,
                                               double maxValueX, double maxValueY,
                                               double expectedX, double expectedY
        )
        {
            var minValue = new Vector2d(minValueX, minValueY);
            var maxValue = new Vector2d(maxValueX, maxValueY);
            var expected = new Vector2d(expectedX, expectedY);

            var bindable = new BindableVector2D { MinValue = minValue, MaxValue = maxValue };
            bindable.Parse(value);

            Assert.AreEqual(expected, bindable.Value);
        }
    }
}
