using NUnit.Framework;
using osu.Framework.Graphics.Primitives;
using osuTK;

namespace osu.Framework.Tests.Polygons
{
    [TestFixture]
    public class LineTest
    {
        [Test]
        public void TestIntersectParallelLines()
        {
            var l1 = new Line(Vector2.Zero, Vector2.One);
            var l2 = new Line(new Vector2(0, -1), new Vector2(1, 0));

            (bool success, _) = l1.Intersect(l2);
            Assert.IsFalse(success);

            (success, _) = l2.Intersect(l1);
            Assert.IsFalse(success);

            // Reverse pass
            l1 = new Line(Vector2.One, Vector2.Zero);
            l2 = new Line(new Vector2(1, 0), new Vector2(0, -1));

            (success, _) = l1.Intersect(l2);
            Assert.IsFalse(success);

            (success, _) = l2.Intersect(l1);
            Assert.IsFalse(success);
        }

        [Test]
        public void TestIntersectPerpendicularNonIntersectingLines()
        {
            var l1 = new Line(new Vector2(0, 1), Vector2.One);
            var l2 = new Line(new Vector2(0.5f, 0), new Vector2(0.5f, 0.9f));

            (bool success, _) = l1.Intersect(l2);
            Assert.IsFalse(success);

            (success, _) = l2.Intersect(l1);
            Assert.IsFalse(success);

            l1 = new Line(Vector2.One, new Vector2(0, 1));
            l2 = new Line(new Vector2(0.5f, 0.9f), new Vector2(0.5f, 0));

            (success, _) = l1.Intersect(l2);
            Assert.IsFalse(success);

            (success, _) = l2.Intersect(l1);
            Assert.IsFalse(success);
        }

        [Test]
        public void TestIntersectIntersectingLines()
        {
            var l1 = new Line(Vector2.Zero, Vector2.One);
            var l2 = new Line(new Vector2(1, 0), new Vector2(0, 1));

            (bool success, float t) = l1.Intersect(l2);
            Assert.IsTrue(success);
            Assert.AreEqual(0.5f, t);

            (success, t) = l2.Intersect(l1);
            Assert.IsTrue(success);
            Assert.AreEqual(0.5f, t);

            l1 = new Line(Vector2.One, Vector2.Zero);
            l2 = new Line(new Vector2(0, 1), new Vector2(1, 0));

            (success, t) = l1.Intersect(l2);
            Assert.IsTrue(success);
            Assert.AreEqual(0.5f, t);

            (success, t) = l2.Intersect(l1);
            Assert.IsTrue(success);
            Assert.AreEqual(0.5f, t);
        }

        [Test]
        public void TestIntersectIntersectionAtEndPoint()
        {
            var l1 = new Line(Vector2.Zero, Vector2.One);
            var l2 = new Line(new Vector2(1, 0), Vector2.One);

            (bool success, float t) = l1.Intersect(l2);
            Assert.IsTrue(success);
            Assert.AreEqual(1, t);

            (success, t) = l2.Intersect(l1);
            Assert.IsTrue(success);
            Assert.AreEqual(1, t);

            l1 = new Line(Vector2.One, Vector2.Zero);
            l2 = new Line(Vector2.One, new Vector2(1, 0));

            (success, t) = l1.Intersect(l2);
            Assert.IsTrue(success);
            Assert.AreEqual(0, t);

            (success, t) = l2.Intersect(l1);
            Assert.IsTrue(success);
            Assert.AreEqual(0, t);
        }

        [Test]
        public void TestIntersectIntersectionAtStartPoint()
        {
            var l1 = new Line(Vector2.Zero, Vector2.One);
            var l2 = new Line(Vector2.One, new Vector2(1, 0));

            (bool success, float t) = l1.Intersect(l2);
            Assert.IsTrue(success);
            Assert.AreEqual(1, t);

            (success, t) = l2.Intersect(l1);
            Assert.IsTrue(success);
            Assert.AreEqual(0, t);

            l1 = new Line(Vector2.One, Vector2.Zero);
            l2 = new Line(new Vector2(1, 0), Vector2.One);

            (success, t) = l1.Intersect(l2);
            Assert.IsTrue(success);
            Assert.AreEqual(0, t);

            (success, t) = l2.Intersect(l1);
            Assert.IsTrue(success);
            Assert.AreEqual(1, t);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TestIsInsideVertical(bool inverse)
        {
            var line = new Line(inverse ? Vector2.One : new Vector2(1, 0), inverse ? new Vector2(1, 0) : Vector2.One);

            // Colinear (independent of inverse)
            Assert.IsTrue(line.IsInside(new Vector2(1, 0.5f)));
            Assert.IsTrue(line.IsInside(new Vector2(1, 1f)));

            // Directly right of line
            Assert.AreEqual(!inverse, line.IsInside(new Vector2(1.5f, 0.25f)));
            Assert.AreEqual(!inverse, line.IsInside(new Vector2(1.5f, 0.5f)));
            Assert.AreEqual(!inverse, line.IsInside(new Vector2(1.5f, 0.75f)));

            // Directly left of line
            Assert.AreEqual(inverse, line.IsInside(new Vector2(0.5f, 0.25f)));
            Assert.AreEqual(inverse, line.IsInside(new Vector2(0.5f, 0.5f)));
            Assert.AreEqual(inverse, line.IsInside(new Vector2(0.5f, 0.75f)));

            // Right of line at y-extrema
            Assert.AreEqual(!inverse, line.IsInside(new Vector2(1.5f, -100f)));
            Assert.AreEqual(!inverse, line.IsInside(new Vector2(1.5f, 100f)));

            // Left of line at y-extrema
            Assert.AreEqual(inverse, line.IsInside(new Vector2(0.5f, -100f)));
            Assert.AreEqual(inverse, line.IsInside(new Vector2(0.5f, 100f)));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TestIsInsideHorizontal(bool inverse)
        {
            var line = new Line(inverse ? Vector2.One : new Vector2(0, 1), inverse ? new Vector2(0, 1) : Vector2.One);

            // Colinear (independent of inverse)
            Assert.IsTrue(line.IsInside(new Vector2(0.5f, 1)));
            Assert.IsTrue(line.IsInside(new Vector2(1f, 1)));

            // Directly right of line
            Assert.AreEqual(!inverse, line.IsInside(new Vector2(0.25f, 0.5f)));
            Assert.AreEqual(!inverse, line.IsInside(new Vector2(0.5f, 0.5f)));
            Assert.AreEqual(!inverse, line.IsInside(new Vector2(0.75f, 0.5f)));

            // Directly last of line
            Assert.AreEqual(inverse, line.IsInside(new Vector2(0.25f, 1.5f)));
            Assert.AreEqual(inverse, line.IsInside(new Vector2(0.5f, 1.5f)));
            Assert.AreEqual(inverse, line.IsInside(new Vector2(0.75f, 1.5f)));

            // Right of line at x-extrema
            Assert.AreEqual(!inverse, line.IsInside(new Vector2(-100f, 0.5f)));
            Assert.AreEqual(!inverse, line.IsInside(new Vector2(100f, 0.5f)));

            // Left of line at x-extrema
            Assert.AreEqual(inverse, line.IsInside(new Vector2(-100f, 1.5f)));
            Assert.AreEqual(inverse, line.IsInside(new Vector2(100f, 1.5f)));
        }
    }
}
