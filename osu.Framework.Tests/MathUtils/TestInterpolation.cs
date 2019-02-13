﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using NUnit.Framework;
using osu.Framework.MathUtils;
using osuTK;

namespace osu.Framework.Tests.MathUtils
{
    [TestFixture]
    public class TestInterpolation
    {
        [Test]
        public void TestLerp()
        {
            Assert.AreEqual(5, Interpolation.Lerp(0, 10, 0.5f));
            Assert.AreEqual(0, Interpolation.Lerp(0, 10, 0));
            Assert.AreEqual(10, Interpolation.Lerp(0, 10, 1));
        }

        [Test]
        public void TestLagrange()
        {
            // lagrange of (0,0) (0.5,0.35) (1,1) is equal to 0.6x*x + 0.4x
            Vector2[] points = { new Vector2(0, 0), new Vector2(0.5f, 0.35f), new Vector2(1, 1) };

            for (double t = -10; t <= 10; t += 0.01)
                Assert.AreEqual(0.6 * t * t + 0.4 * t, Interpolation.Lagrange(points, t), 1e-4);

            Assert.Throws<ArgumentException>(() =>
            {
                Interpolation.Lagrange(null, 4);
            });

            Assert.Throws<ArgumentException>(() =>
            {
                ReadOnlySpan<Vector2> list = new Vector2[0];
                Interpolation.Lagrange(list, 4);
            });

            Assert.AreEqual(Interpolation.Lagrange(new[] { new Vector2(3, 4) }, 12), 4);

            double[] weights = Interpolation.BarycentricWeights(points);
            Assert.AreEqual(3, weights.Length);
            Assert.AreEqual(2, weights[0]);
            Assert.AreEqual(-4, weights[1]);
            Assert.AreEqual(2, weights[2]);
        }
    }
}
