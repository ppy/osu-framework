// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections;
using NUnit.Framework;
using osu.Framework.Extensions.MatrixExtensions;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Utils;
using osuTK;

namespace osu.Framework.Tests.Primitives
{
    [TestFixture]
    public class QuadTest
    {
        private static readonly object[] test_quads =
        {
            new Quad(0, 0, 10, 10),
            // Test potential precision edge cases using the precise centre point.
            new Quad(
                new Vector2(1513.5333f, 695.51416f),
                new Vector2(1679.6332f, 695.51416f),
                new Vector2(1513.5333f, 861.614f),
                new Vector2(1679.6332f, 861.614f)
            ),
            // Arbitrary convex quad
            new Quad(
                new Vector2(3, 0),
                new Vector2(5, 1),
                new Vector2(0, 5),
                new Vector2(7, 7)
            ),
            // Very small quad
            new Quad(
                new Vector2(-Precision.FLOAT_EPSILON, -Precision.FLOAT_EPSILON),
                new Vector2(Precision.FLOAT_EPSILON, -Precision.FLOAT_EPSILON),
                new Vector2(-Precision.FLOAT_EPSILON, Precision.FLOAT_EPSILON),
                new Vector2(Precision.FLOAT_EPSILON, Precision.FLOAT_EPSILON)
            )
        };

        [TestCaseSource(typeof(AreaTestData), nameof(AreaTestData.TestCases))]
        [DefaultFloatingPointTolerance(0.1f)]
        public float TestArea(Quad testQuad) => testQuad.Area;

        [TestCaseSource(nameof(test_quads))]
        public void TestContains(Quad quad)
        {
            Assert.That(quad.Contains(new Vector2(float.MinValue)), Is.False);
            Assert.That(quad.Contains(new Vector2(float.MaxValue)), Is.False);

            Assert.That(quad.Contains(quad.TopLeft), Is.True);
            Assert.That(quad.Contains(quad.TopRight), Is.True);
            Assert.That(quad.Contains(quad.BottomLeft), Is.True);
            Assert.That(quad.Contains(quad.BottomRight), Is.True);
            Assert.That(quad.Contains(quad.Centre), Is.True);

            Assert.That(quad.Contains(quad.TopLeft + new Vector2(-1, 0)), Is.False);
            Assert.That(quad.Contains(quad.TopLeft + new Vector2(0, -1)), Is.False);
            Assert.That(quad.Contains(quad.TopLeft + new Vector2(-1, -1)), Is.False);

            Assert.That(quad.Contains(quad.TopRight + new Vector2(1, 0)), Is.False);
            Assert.That(quad.Contains(quad.TopRight + new Vector2(0, -1)), Is.False);
            Assert.That(quad.Contains(quad.TopRight + new Vector2(1, -1)), Is.False);

            Assert.That(quad.Contains(quad.BottomLeft + new Vector2(-1, 0)), Is.False);
            Assert.That(quad.Contains(quad.BottomLeft + new Vector2(0, 1)), Is.False);
            Assert.That(quad.Contains(quad.BottomLeft + new Vector2(-1, 1)), Is.False);

            Assert.That(quad.Contains(quad.BottomRight + new Vector2(1, 0)), Is.False);
            Assert.That(quad.Contains(quad.BottomRight + new Vector2(0, 1)), Is.False);
            Assert.That(quad.Contains(quad.BottomRight + new Vector2(1)), Is.False);

            Assert.That(quad.Contains((quad.TopLeft + quad.TopRight) / 2), Is.True);
            Assert.That(quad.Contains((quad.BottomLeft + quad.BottomRight) / 2), Is.True);
            Assert.That(quad.Contains((quad.TopLeft + quad.BottomLeft) / 2), Is.True);
            Assert.That(quad.Contains((quad.TopRight + quad.BottomRight) / 2), Is.True);
        }

        [Test]
        public void TestContains_ZeroHeightQuad()
        {
            var quad = new Quad(-2, 0, 4, 0);

            Assert.That(quad.Contains(new Vector2(-5, -5)), Is.False);
            Assert.That(quad.Contains(new Vector2(-5, 5)), Is.False);
            Assert.That(quad.Contains(new Vector2(5, -5)), Is.False);
            Assert.That(quad.Contains(new Vector2(5, 5)), Is.False);

            Assert.That(quad.Contains(new Vector2(-2, 0)), Is.True);
            Assert.That(quad.Contains(new Vector2(0, 0)), Is.True);
            Assert.That(quad.Contains(new Vector2(2, 0)), Is.True);
        }

        [Test]
        public void TestContains_ZeroWidthQuad()
        {
            var quad = new Quad(0, -2, 0, 4);

            Assert.That(quad.Contains(new Vector2(-5, -5)), Is.False);
            Assert.That(quad.Contains(new Vector2(-5, 5)), Is.False);
            Assert.That(quad.Contains(new Vector2(5, -5)), Is.False);
            Assert.That(quad.Contains(new Vector2(5, 5)), Is.False);

            Assert.That(quad.Contains(new Vector2(0, -2)), Is.True);
            Assert.That(quad.Contains(new Vector2(0, 0)), Is.True);
            Assert.That(quad.Contains(new Vector2(0, 2)), Is.True);
        }

        [Test]
        public void TestContains_ZeroSizedQuad()
        {
            var quad = new Quad(0, 0, 0, 0);

            Assert.That(quad.Contains(new Vector2(-5, -5)), Is.False);
            Assert.That(quad.Contains(new Vector2(-5, 5)), Is.False);
            Assert.That(quad.Contains(new Vector2(5, -5)), Is.False);
            Assert.That(quad.Contains(new Vector2(5, 5)), Is.False);

            Assert.That(quad.Contains(new Vector2(0, 0)), Is.True);
        }

        [Test]
        public void TestFromNegativeSizedRectangle()
        {
            var rectangle = new RectangleF(-5, 5, 10, -10);
            var quad = Quad.FromRectangle(rectangle);

            Assert.That(quad.Contains(new Vector2(0, 0)), Is.True);

            Assert.That(quad.Contains(new Vector2(-5, -5)), Is.True);
            Assert.That(quad.Contains(new Vector2(-5, 5)), Is.True);
            Assert.That(quad.Contains(new Vector2(5, -5)), Is.True);
            Assert.That(quad.Contains(new Vector2(5, 5)), Is.True);

            Assert.That(quad.Contains(new Vector2(-15, -15)), Is.False);
            Assert.That(quad.Contains(new Vector2(-15, 15)), Is.False);
            Assert.That(quad.Contains(new Vector2(15, -15)), Is.False);
            Assert.That(quad.Contains(new Vector2(15, 15)), Is.False);
        }

        [Test]
        public void TestFlippedQuad()
        {
            var quad = new Quad(-5, -5, 10, 10);
            quad *= new Matrix3(
                -1, 0, 0,
                0, 1, 0,
                0, 0, 1);

            Assert.That(quad.Contains(new Vector2(0, 0)), Is.True);

            Assert.That(quad.Contains(new Vector2(-5, -5)), Is.True);
            Assert.That(quad.Contains(new Vector2(-5, 5)), Is.True);
            Assert.That(quad.Contains(new Vector2(5, -5)), Is.True);
            Assert.That(quad.Contains(new Vector2(5, 5)), Is.True);

            Assert.That(quad.Contains(new Vector2(-15, -15)), Is.False);
            Assert.That(quad.Contains(new Vector2(-15, 15)), Is.False);
            Assert.That(quad.Contains(new Vector2(15, -15)), Is.False);
            Assert.That(quad.Contains(new Vector2(15, 15)), Is.False);
        }

        private class AreaTestData
        {
            public static IEnumerable TestCases
            {
                get
                {
                    // Point
                    yield return new TestCaseData(new Quad(0, 0, 0, 0)).Returns(0);

                    // Lines
                    yield return new TestCaseData(new Quad(0, 0, 100, 0)).Returns(0);
                    yield return new TestCaseData(new Quad(0, 0, 0, 100)).Returns(0);
                    yield return new TestCaseData(new Quad(0, 0, 100, 1)).Returns(100);
                    yield return new TestCaseData(new Quad(0, 0, 1, 100)).Returns(100);

                    // Simple quads
                    yield return new TestCaseData(new Quad(0, 0, 10, 10)).Returns(100);
                    yield return new TestCaseData(new Quad(0, 0, 10, 5)).Returns(50);
                    yield return new TestCaseData(new Quad(0, 0, 5, 10)).Returns(50);

                    // Rotated simple quads
                    yield return new TestCaseData(new Quad(10, 10, -10, -10)).Returns(100);
                    yield return new TestCaseData(new Quad(10, 5, -10, -5)).Returns(50);
                    yield return new TestCaseData(new Quad(5, 10, -5, -10)).Returns(50);

                    // Sheared quads
                    yield return new TestCaseData(shear(new Quad(0, 0, 10, 10), new Vector2(2))).Returns(100);
                    yield return new TestCaseData(shear(new Quad(0, 0, 10, 10), new Vector2(-2))).Returns(100);

                    // Sheared rotated quads
                    yield return new TestCaseData(shear(new Quad(10, 10, -10, -10), new Vector2(2))).Returns(100);
                    yield return new TestCaseData(shear(new Quad(10, 5, -10, -5), new Vector2(2))).Returns(50);
                    yield return new TestCaseData(shear(new Quad(5, 10, -5, -10), new Vector2(2))).Returns(50);

                    // Self-intersecting quads
                    yield return new TestCaseData(new Quad(new Vector2(0, 5), new Vector2(0, -5), new Vector2(-5, 0), new Vector2(5, 0))).Returns(0);
                    yield return new TestCaseData(new Quad(new Vector2(0, 5), new Vector2(0, -5), Vector2.Zero, new Vector2(5, 0))).Returns(12.5f);
                }
            }

            private static Quad shear(Quad quad, Vector2 amount)
            {
                var matrix = Matrix3.Identity;
                MatrixExtensions.ShearFromLeft(ref matrix, Vector2.Divide(amount, quad.Size));

                return quad * matrix;
            }
        }
    }
}
