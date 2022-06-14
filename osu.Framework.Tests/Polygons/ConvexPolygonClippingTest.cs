// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Utils;
using osuTK;

namespace osu.Framework.Tests.Polygons
{
    [TestFixture]
    public class ConvexPolygonClippingTest
    {
        private static readonly Vector2 origin = Vector2.Zero;
        private static readonly Vector2 up_1 = new Vector2(0, 1);
        private static readonly Vector2 up_2 = new Vector2(0, 2);
        private static readonly Vector2 up_3 = new Vector2(0, 3);
        private static readonly Vector2 down_1 = new Vector2(0, -1);
        private static readonly Vector2 down_2 = new Vector2(0, -2);
        private static readonly Vector2 down_3 = new Vector2(0, -3);
        private static readonly Vector2 left_1 = new Vector2(-1, 0);
        private static readonly Vector2 left_2 = new Vector2(-2, 0);
        private static readonly Vector2 left_3 = new Vector2(-3, 0);
        private static readonly Vector2 right_1 = new Vector2(1, 0);
        private static readonly Vector2 right_2 = new Vector2(2, 0);
        private static readonly Vector2 right_3 = new Vector2(3, 0);

        private static object[] externalTestCases => new object[]
        {
            // Non-rotated
            new object[] { new[] { origin, up_1, up_1 + right_1, right_1 }, new[] { up_2, up_3, up_3 + right_1, up_2 } },
            new object[] { new[] { origin, up_1, up_1 + right_1, right_1 }, new[] { right_2, right_2 + up_1, right_3 + up_1, right_3 } },
            new object[] { new[] { origin, up_1, up_1 + right_1, right_1 }, new[] { down_1, down_1 + right_1, down_2 + right_1, down_2 } },
            new object[] { new[] { origin, up_1, up_1 + right_1, right_1 }, new[] { left_2, left_2 + up_1, left_1 + up_1, left_1 } },
            // Rotated
            new object[] { new[] { origin, up_1, up_1 + right_1, right_1 }, new[] { up_1 + right_2, up_2 + right_1, up_3 + right_2, up_2 + right_3 } },
            new object[] { new[] { origin, up_1, up_1 + right_1, right_1 }, new[] { up_1 + right_2, down_1 + right_3, down_2 + right_2, down_1 + right_1 } },
            new object[] { new[] { origin, up_1, up_1 + right_1, right_1 }, new[] { down_1 + right_1, down_2 + right_2, down_3 + right_1, down_2 } },
            new object[] { new[] { origin, up_1, up_1 + right_1, right_1 }, new[] { left_1 + up_1, down_2, down_3 + left_2, left_2 } },
        };

        [TestCaseSource(nameof(externalTestCases))]
        public void TestExternalPolygon(Vector2[] polygonVertices1, Vector2[] polygonVertices2)
        {
            var poly1 = new SimpleConvexPolygon(polygonVertices1);
            var poly2 = new SimpleConvexPolygon(polygonVertices2);

            Assert.That(clip(poly1, poly2).Length, Is.Zero);
            Assert.That(clip(poly2, poly1).Length, Is.Zero);

            Array.Reverse(polygonVertices1);
            Array.Reverse(polygonVertices2);

            Assert.That(clip(poly1, poly2).Length, Is.Zero);
            Assert.That(clip(poly2, poly1).Length, Is.Zero);
        }

        private static object[] subjectFullyContainedTestCases => new object[]
        {
            // Same polygon
            new object[] { new[] { origin, up_2, up_2 + right_2, right_2 }, new[] { origin, up_2, up_2 + right_2, right_2 } },
            new object[] { new[] { down_2, left_2, up_2, right_2 }, new[] { down_2, left_2, up_2, right_2 } },
            // Corners
            new object[] { new[] { origin, up_2, up_2 + right_2, right_2 }, new[] { origin, up_1, up_1 + right_1, right_1 } },
            new object[] { new[] { origin, up_2, up_2 + right_2, right_2 }, new[] { up_1, up_2, up_2 + right_1, up_1 + right_1 } },
            new object[] { new[] { origin, up_2, up_2 + right_2, right_2 }, new[] { up_1 + right_1, up_2 + right_1, up_2 + right_2, up_1 + right_2 } },
            new object[] { new[] { origin, up_2, up_2 + right_2, right_2 }, new[] { right_1, up_1 + right_1, up_1 + right_2, right_2 } },
            new object[] { new[] { down_2, left_2, up_2, right_2 }, new[] { down_2, down_1, right_1, right_2 } },
            new object[] { new[] { down_2, left_2, up_2, right_2 }, new[] { left_2, left_1, down_1, down_2 } },
            new object[] { new[] { down_2, left_2, up_2, right_2 }, new[] { left_2, up_2, up_1, left_1 } },
            new object[] { new[] { down_2, left_2, up_2, right_2 }, new[] { up_2, right_2, right_1, up_1 } },
            // Padded
            new object[] { new[] { origin, up_2, up_2 + right_2, right_2 }, new[] { right_1 * 0.5f + up_1 * 0.5f, up_2 * 0.75f, up_2 * 0.75f + right_2 * 0.75f, right_2 * 0.75f } },
            new object[] { new[] { down_2, left_2, up_2, right_2 }, new[] { down_1, left_1, up_1, right_1 } },
            // Rotated
            new object[] { new[] { origin, up_2, up_2 + right_2, right_2 }, new[] { up_1 + right_1 * 0.5f, up_2 * 0.75f + right_1, up_1 + right_2 * 0.5f, up_1 * 0.5f + right_1 } },
            new object[] { new[] { origin, up_2, up_2 + right_2, right_2 }, new[] { up_1, up_2 * 0.75f, up_2 + right_1 * 0.5f, up_2 + right_1 } },
            new object[] { new[] { origin, up_2, up_2 + right_2, right_2 }, new[] { right_1, up_1 + right_2, up_1 * 0.5f + right_2, right_2 * 0.75f } },
            new object[] { new[] { down_2, left_2, up_2, right_2 }, new[] { left_1 + up_1, up_1 + right_1, down_1 + right_1, left_1 + down_1 } },
        };

        [TestCaseSource(nameof(subjectFullyContainedTestCases))]
        public void TestSubjectFullyContained(Vector2[] clipVertices, Vector2[] subjectVertices)
        {
            var clipPolygon = new SimpleConvexPolygon(clipVertices);
            var subjectPolygon = new SimpleConvexPolygon(subjectVertices);

            assertPolygonEquals(subjectPolygon, new SimpleConvexPolygon(clip(clipPolygon, subjectPolygon).ToArray()), false);

            Array.Reverse(clipVertices);
            Array.Reverse(subjectVertices);

            assertPolygonEquals(subjectPolygon, new SimpleConvexPolygon(clip(clipPolygon, subjectPolygon).ToArray()), true);
        }

        [TestCaseSource(nameof(subjectFullyContainedTestCases))]
        public void TestClipFullyContained(Vector2[] subjectVertices, Vector2[] clipVertices)
        {
            var clipPolygon = new SimpleConvexPolygon(clipVertices);
            var subjectPolygon = new SimpleConvexPolygon(subjectVertices);

            assertPolygonEquals(clipPolygon, new SimpleConvexPolygon(clip(clipPolygon, subjectPolygon).ToArray()), false);

            Array.Reverse(clipVertices);
            Array.Reverse(subjectVertices);

            assertPolygonEquals(clipPolygon, new SimpleConvexPolygon(clip(clipPolygon, subjectPolygon).ToArray()), true);
        }

        private static object[] generalClippingTestCases => new object[]
        {
            new object[]
            {
                new[] { origin, up_2, up_2 + right_2, right_2 }, new[] { left_1 + up_1, up_1 + right_1, down_1 + right_1, left_1 + down_1 }, new[] { origin, up_1, up_1 + right_1, right_1 }
            },
            new object[] { new[] { origin, up_2, up_2 + right_2, right_2 }, new[] { left_1, up_1, right_1, down_1 }, new[] { origin, up_1, right_1 } },
            new object[]
            {
                new[] { origin, up_2, up_2 + right_2, right_2 }, new[] { up_1 + right_1, up_3 + right_1, up_3 + right_3, up_1 + right_3 },
                new[] { up_1 + right_1, up_2 + right_1, up_2 + right_2, up_1 + right_2 }
            },
            new object[]
            {
                new[] { origin, up_2, up_2 + right_2, right_2 }, new[] { up_2 + right_1, up_3 + right_2, up_2 + right_3, up_1 + right_2 }, new[] { up_2 + right_1, up_2 + right_2, up_1 + right_2 }
            },
            new object[] { new[] { origin, up_2, up_2 + right_2, right_2 }, new[] { left_1 + up_1, up_3 + right_1, up_1 + right_1, origin }, new[] { up_2, up_2 + right_1, up_1 + right_1, origin } },
            new object[]
            {
                new[] { origin, up_2, up_2 + right_2, right_2 }, new[] { left_1 + up_1, up_1 + right_3, down_1 + right_3, left_1 + down_1 }, new[] { up_1, up_1 + right_2, right_2, origin }
            },
            new object[]
            {
                new[] { origin, up_2, up_2 + right_2, right_2 }, new[] { down_1 + left_1, up_3 + left_1, up_3 + right_1, down_1 + right_1 }, new[] { origin, up_2, up_2 + right_1, right_1 }
            },
            new object[]
            {
                new[] { origin, up_2, up_2 + right_2, right_2 }, new[] { down_1, up_1 + right_1, down_1 + right_2, down_2 + right_1 }, new[] { right_1 * 0.5f, up_1 + right_1, right_2 * 0.75f }
            },
            new object[] { new[] { origin, up_2, up_2 + right_2, right_2 }, new[] { origin, up_3 + right_3, right_2 }, new[] { origin, up_2 + right_2, right_2 } },
            new object[] { new[] { up_2, right_2, down_2, left_2 }, new[] { left_1 + down_2, left_2 + down_2, up_2 + left_2, up_2 + left_1 }, new[] { up_1 + left_1, down_1 + left_1, left_2 } },
            new object[] { new[] { up_2, right_2, down_2, left_2 }, new[] { origin, down_2 + left_2, up_2 + left_2 }, new[] { origin, down_1 + left_1, left_2, up_1 + left_1 } },
            new object[] { new[] { up_2, right_2, down_2, left_2 }, new[] { origin, left_3, up_3 }, new[] { origin, left_2, up_2 } },
            new object[] { new[] { up_2, right_2, down_2, left_2 }, new[] { origin, left_3, up_3, right_3 }, new[] { origin, left_2, up_2, right_2 } },
            new object[]
            {
                new[] { left_1 + up_1, right_1 + up_1, down_1 + right_1, down_1 + left_1 }, new[] { up_2 * 0.75f, right_2 * 0.75f, down_2 * 0.75f, left_2 * 0.75f },
                new[]
                {
                    down_1 + left_1 * 0.5f,
                    down_1 * 0.5f + left_1,
                    up_1 * 0.5f + left_1,
                    up_1 + left_1 * 0.5f,
                    up_1 + right_1 * 0.5f,
                    up_1 * 0.5f + right_1,
                    down_1 * 0.5f + right_1,
                    down_1 + right_1 * 0.5f
                }
            },
            new object[]
            {
                new[] { up_1, right_1, left_1 }, new[] { up_1 + right_1 * 0.5f, down_1 + right_1 * 0.5f, down_1 + left_1 * 0.5f, up_1 + left_1 * 0.5f },
                new[] { up_1 * 0.5f + left_1 * 0.5f, up_1, up_1 * 0.5f + right_1 * 0.5f, right_1 * 0.5f, left_1 * 0.5f, }
            },
            new object[]
            {
                // Inverse of the above
                new[] { up_1 + right_1 * 0.5f, down_1 + right_1 * 0.5f, down_1 + left_1 * 0.5f, up_1 + left_1 * 0.5f }, new[] { up_1, right_1, left_1 },
                new[] { up_1 * 0.5f + left_1 * 0.5f, up_1, up_1 * 0.5f + right_1 * 0.5f, right_1 * 0.5f, left_1 * 0.5f, }
            },
            new object[]
            {
                new[] { up_1, right_1, down_1 + right_1, down_1 + left_1, left_1 }, new[] { left_1, up_1, up_1 + right_1, down_1 + right_1, down_1 },
                new[] { up_1, right_1, right_1 + down_1, down_1, left_1 }
            }
        };

        [TestCaseSource(nameof(generalClippingTestCases))]
        public void TestGeneralClipping(Vector2[] clipVertices, Vector2[] subjectVertices, Vector2[] resultingVertices)
        {
            var clipPolygon = new SimpleConvexPolygon(clipVertices);
            var subjectPolygon = new SimpleConvexPolygon(subjectVertices);

            assertPolygonEquals(new SimpleConvexPolygon(resultingVertices), new SimpleConvexPolygon(clip(clipPolygon, subjectPolygon).ToArray()), false);

            Array.Reverse(clipVertices);
            Array.Reverse(subjectVertices);

            // The expected polygon is never reversed
            assertPolygonEquals(new SimpleConvexPolygon(resultingVertices), new SimpleConvexPolygon(clip(clipPolygon, subjectPolygon).ToArray()), false);
        }

        [Test]
        public void TestTriangleClipping()
        {
            assertPolygonEquals(new SimpleConvexPolygon(new[] { Vector2.Zero, new Vector2(0, 1), new Vector2(1, 0) }),
                new SimpleConvexPolygon(clip(new Quad(Vector2.Zero, Vector2.Zero, new Vector2(1, 0), new Vector2(0, 1)), new Quad(0, 0, 10, 10)).ToArray()),
                false);
        }

        [Test]
        public void TestLineClipping()
        {
            assertPolygonEquals(new SimpleConvexPolygon(Array.Empty<Vector2>()),
                new SimpleConvexPolygon(clip(new Quad(25, 25, 0, 10), new Quad(0, 0, 100, 100)).ToArray()),
                false);

            assertPolygonEquals(new SimpleConvexPolygon(Array.Empty<Vector2>()),
                new SimpleConvexPolygon(clip(new Quad(25, 25, 10, 0), new Quad(0, 0, 100, 100)).ToArray()),
                false);
        }

        [Test]
        public void TestPointClipping()
        {
            assertPolygonEquals(new SimpleConvexPolygon(Array.Empty<Vector2>()),
                new SimpleConvexPolygon(clip(new Quad(25, 25, 0, 0), new Quad(0, 0, 100, 100)).ToArray()),
                false);
        }

        [Test]
        public void TestEmptyClip()
        {
            var quad = new Quad(0, 0, 1, 1);

            assertPolygonEquals(
                new SimpleConvexPolygon(Array.Empty<Vector2>()),
                new SimpleConvexPolygon(clip(new SimpleConvexPolygon(Array.Empty<Vector2>()), quad).ToArray()),
                false);
        }

        [Test]
        public void TestEmptySubject()
        {
            var quad = new Quad(0, 0, 1, 1);

            assertPolygonEquals(
                new SimpleConvexPolygon(Array.Empty<Vector2>()),
                new SimpleConvexPolygon(clip(quad, new SimpleConvexPolygon(Array.Empty<Vector2>())).ToArray()),
                false);
        }

        private static object[] fuzzedEdgeCases => new object[]
        {
            new object[]
            {
                new[] { new Vector2(0, 0.5f), new Vector2(100, 1), new Vector2(2, 2), new Vector2(1, 2) },
                new[] { new Vector2(0, 0.5f), new Vector2(100, 0), new Vector2(0.5f, 100) },
            },
            new object[]
            {
                new[] { new Vector2(0, 1), new Vector2(100, 0), new Vector2(100, 2), new Vector2(2, 2) },
                new[] { new Vector2(1, 100), new Vector2(2, 0.5f), new Vector2(100, 2) },
            },
            new object[]
            {
                new[] { new Vector2(0, 1), new Vector2(2, 0.5f), new Vector2(100, 0), new Vector2(1, 2) },
                new[] { new Vector2(1, 0.5f), new Vector2(100, 0), new Vector2(1, 100) },
            },
            new object[]
            {
                new[] { new Vector2(0, 1), new Vector2(0, 0), new Vector2(2, 1), new Vector2(1, 2) },
                new[] { new Vector2(0.5f, 2), new Vector2(100, 0.5f), new Vector2(1, 0.5f), new Vector2(100, 2) },
            },
            new object[]
            {
                new[] { new Vector2(2, float.MinValue), new Vector2(float.MaxValue, 0.5f), new Vector2(float.MaxValue, float.NegativeInfinity), new Vector2(1, float.MaxValue) },
                new[] { new Vector2(0, 1), new Vector2(0, float.NegativeInfinity), new Vector2(float.Epsilon, 1), new Vector2(1, 0.5f) },
            },
            new object[]
            {
                new[] { new Vector2(float.Epsilon, 0.5f), new Vector2(float.Epsilon, 100), new Vector2(float.MaxValue, float.NegativeInfinity), new Vector2(1, float.MaxValue) },
                new[] { new Vector2(0, 1), new Vector2(0, float.NegativeInfinity), new Vector2(float.Epsilon, 1), new Vector2(1, 0.5f) },
            },
            new object[]
            {
                new[] { new Vector2(-0.1f, 2), new Vector2(0, 0), new Vector2(0.5f, -0.1f) },
                new[] { new Vector2(-10, 2), new Vector2(-0.5f, 0.1f), new Vector2(0.5f, 0.5f) }
            },
            new object[]
            {
                new[] { new Vector2(-10, 1), new Vector2(0.1f, 0), new Vector2(-0.5f, 2) },
                new[] { new Vector2(-1, 2), new Vector2(-0.1f, -0.5f), new Vector2(0.1f, 0) }
            },
            new object[]
            {
                new[] { new Vector2(-1, 0.5f), new Vector2(0.1f, 0.1f), new Vector2(0, 1) },
                new[] { new Vector2(-2, -0.5f), new Vector2(0.1f, 0.1f), new Vector2(-0.1f, 1) }
            }
        };

        [TestCaseSource(nameof(fuzzedEdgeCases))]
        public void TestFuzzedEdgeCases(Vector2[] clipVertices, Vector2[] subjectVertices)
        {
            var clipPolygon = new SimpleConvexPolygon(clipVertices);
            var subjectPolygon = new SimpleConvexPolygon(subjectVertices);

            clip(clipPolygon, subjectPolygon);
        }

        private Span<Vector2> clip(SimpleConvexPolygon clipPolygon, SimpleConvexPolygon subjectPolygon)
            => new ConvexPolygonClipper<SimpleConvexPolygon, SimpleConvexPolygon>(ref clipPolygon, ref subjectPolygon).Clip();

        private Span<Vector2> clip<TClip, TSubject>(TClip clipPolygon, TSubject subjectPolygon)
            where TClip : IConvexPolygon
            where TSubject : IConvexPolygon
            => new ConvexPolygonClipper<TClip, TSubject>(ref clipPolygon, ref subjectPolygon).Clip();

        private void assertPolygonEquals(IPolygon expected, IPolygon actual, bool reverse)
            => Assert.That(Vector2Extensions.GetOrientation(actual.GetVertices()),
                reverse
                    ? Is.EqualTo(-Vector2Extensions.GetOrientation(expected.GetVertices()))
                    : Is.EqualTo(Vector2Extensions.GetOrientation(expected.GetVertices())));
    }
}
