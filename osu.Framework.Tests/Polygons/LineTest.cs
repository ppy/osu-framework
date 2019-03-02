using NUnit.Framework;
using osu.Framework.Graphics.Primitives;
using osuTK;

namespace osu.Framework.Tests.Polygons
{
    [TestFixture]
    public class LineTest
    {
        [Test]
        public void TestParallelLines()
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
        public void TestPerpendicularNonIntersectingLines()
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
        public void TestIntersectingLines()
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
        public void TestIntersectionAtEndPoint()
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
        public void TestIntersectionAtStartPoint()
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
    }
}
