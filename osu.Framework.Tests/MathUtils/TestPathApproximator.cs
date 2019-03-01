// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using NUnit.Framework;
using osu.Framework.MathUtils;
using osuTK;

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

            List<Vector2> approximated = PathApproximator.ApproximateLagrangePolynomial(points);
            Assert.Greater(approximated.Count, 10, "Approximated polynomial should have at least 10 points to test");

            for (int i = 0; i < approximated.Count; i++)
            {
                float x = approximated[i].X;
                Assert.GreaterOrEqual(x, 0);
                Assert.LessOrEqual(x, 1);
                Assert.AreEqual(0.6f * x * x + 0.4f * x, approximated[i].Y, 1e-4);
            }
        }
    }
}
