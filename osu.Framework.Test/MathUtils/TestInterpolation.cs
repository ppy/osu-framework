using System;
using osu.Framework.MathUtils;
using Xunit;

namespace osu.Framework.Test.MathUtils
{
    public class TestInterpolation
    {
        [Fact]
        public void TestLerp()
        {
            Assert.Equal(5, Interpolation.Lerp(0, 10, 0.5f));
            Assert.Equal(0, Interpolation.Lerp(0, 10, 0));
            Assert.Equal(10, Interpolation.Lerp(0, 10, 1));
        }
    }
}

