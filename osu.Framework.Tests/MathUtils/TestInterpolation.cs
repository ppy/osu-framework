// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using NUnit.Framework;
using osu.Framework.MathUtils;

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
            double[] x = new double[] {0, 0.5, 1};
            double[] y = new double[] {0, 0.35, 1};

            // lagrange of (0,0) (0.5,0.35) (1,1) is equal to 0.6x*x + 0.4x

            for (double t = -10; t <= 10; t += 0.01)
                Assert.AreEqual(0.6 * t * t + 0.4 * t, Interpolation.Lagrange(x, y, t), 1e-6);
        }
    }
}
