using System;
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
    }
}

