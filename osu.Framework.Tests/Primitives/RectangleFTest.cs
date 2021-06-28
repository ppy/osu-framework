// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Graphics.Primitives;

namespace osu.Framework.Tests.Primitives
{
    [TestFixture]
    public class RectangleFTest
    {
        [TestCase(0, 0, 0, 0, 10, -10, -10, 20, 20)]
        [TestCase(10, 10, 20, 20, 10, 0, 0, 40, 40)]
        [TestCase(-10, -10, -20, -20, 10, -20, -20, 0, 0)]
        [TestCase(-10, -10, -10, -10, 10, -20, -20, 10, 10)]
        public void TestInflateAndShrink(int x, int y, int width, int height, int amount, int xExpected, int yExpected, int widthExpected, int heightExpected)
        {
            var inflated = new RectangleF(x, y, width, height).Inflate(amount);
            var shrunk = new RectangleF(xExpected, yExpected, widthExpected, heightExpected).Shrink(amount);

            Assert.That(inflated.X, Is.EqualTo(xExpected).Within(0.1f));
            Assert.That(inflated.Y, Is.EqualTo(yExpected).Within(0.1f));
            Assert.That(inflated.Width, Is.EqualTo(widthExpected).Within(0.1f));
            Assert.That(inflated.Height, Is.EqualTo(heightExpected).Within(0.1f));

            Assert.That(shrunk.X, Is.EqualTo(x).Within(0.1f));
            Assert.That(shrunk.Y, Is.EqualTo(y).Within(0.1f));
            Assert.That(shrunk.Width, Is.EqualTo(width).Within(0.1f));
            Assert.That(shrunk.Height, Is.EqualTo(height).Within(0.1f));
        }

        [TestCase(0, 0, 0, 0, 10, -10, -10, 20, 20)]
        [TestCase(10, 10, 20, 20, 10, 0, 0, 40, 40)]
        [TestCase(-10, -10, -20, -20, 10, 0, 0, -40, -40)]
        [TestCase(-10, -10, -10, -10, 10, 0, 0, -30, -30)]
        [TestCase(-10, -10, 0, 0, 10, -20, -20, 20, 20)]
        public void TestDirectionalInflateAndShrink(int x, int y, int width, int height, int amount, int xExpected, int yExpected, int widthExpected, int heightExpected)
        {
            var inflated = new RectangleF(x, y, width, height).DirectionalInflate(amount);
            var shrunk = new RectangleF(xExpected, yExpected, widthExpected, heightExpected).DirectionalShrink(amount);

            Assert.That(inflated.X, Is.EqualTo(xExpected).Within(0.1f));
            Assert.That(inflated.Y, Is.EqualTo(yExpected).Within(0.1f));
            Assert.That(inflated.Width, Is.EqualTo(widthExpected).Within(0.1f));
            Assert.That(inflated.Height, Is.EqualTo(heightExpected).Within(0.1f));

            Assert.That(shrunk.X, Is.EqualTo(x).Within(0.1f));
            Assert.That(shrunk.Y, Is.EqualTo(y).Within(0.1f));
            Assert.That(shrunk.Width, Is.EqualTo(width).Within(0.1f));
            Assert.That(shrunk.Height, Is.EqualTo(height).Within(0.1f));
        }
    }
}
