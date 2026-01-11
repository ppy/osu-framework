// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Utils;
using System.Numerics;

namespace osu.Framework.Tests.MathUtils
{
    [TestFixture]
    public class TestPathApproximator
    {
        [Test]
        public void TestLagrange()
        {
            // lagrange of (0,0) (0.5,0.35) (1,1) is equal to 0.6x*x + 0.4x
            Vector2[] points = { new Vector2(0, 0), new Vector2(0.5f, 0.35f), new Vector2(1, 1) };

            List<Vector2> approximated = PathApproximator.LagrangePolynomialToPiecewiseLinear(points);
            Assert.Greater(approximated.Count, 10, "Approximated polynomial should have at least 10 points to test");

            for (int i = 0; i < approximated.Count; i++)
            {
                float x = approximated[i].X;
                Assert.GreaterOrEqual(x, 0);
                Assert.LessOrEqual(x, 1);
                Assert.AreEqual(0.6f * x * x + 0.4f * x, approximated[i].Y, 1e-4);
            }
        }

        [Test]
        public void TestBSpline()
        {
            Vector2[] points = { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, -1), new Vector2(-1, -1), new Vector2(-1, 1), new Vector2(3, 2), new Vector2(3, 0) };

            List<Vector2> approximated = PathApproximator.BSplineToPiecewiseLinear(points, 4);
            Assert.AreEqual(approximated.Count, 29, "Approximated path should have 29 points to test");
            Assert.True(Precision.AlmostEquals(approximated[0], points[0], 1e-4f));
            Assert.True(Precision.AlmostEquals(approximated[28], points[6], 1e-4f));
            Assert.True(Precision.AlmostEquals(approximated[10], new Vector2(-0.11415f, -0.69065f), 1e-4f));
        }

        [Test]
        public void TestBSplineThrowsOnInvalidDegree()
        {
            Vector2[] points = { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, -1), new Vector2(-1, -1), new Vector2(-1, 1), new Vector2(3, 2), new Vector2(3, 0) };

            Assert.Throws<ArgumentOutOfRangeException>(() => PathApproximator.BSplineToPiecewiseLinear(points, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => PathApproximator.BSplineToPiecewiseLinear(points, -5));
        }

        [TestCase(0)]
        [TestCase(1)]
        public void TestBSplineDoesNothingWhenGivenTooFewPoints(int pointCount)
        {
            var points = Enumerable.Repeat(new Vector2(), pointCount).ToArray();

            Assert.That(PathApproximator.BSplineToPiecewiseLinear(points, 3), Is.EquivalentTo(points));
            Assert.That(PathApproximator.BezierToPiecewiseLinear(points), Is.EquivalentTo(points));
        }
    }
}
