// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using Moq;
using NUnit.Framework;
using osu.Framework.MathUtils;

namespace osu.Framework.Tests.MathUtils
{
    [TestFixture]
    class TestRNG
    {
        [Test]
        public void TestNext()
        {
            Mock<IRandomProvider> mockRandomProvider = new Mock<IRandomProvider>();
            int expectedRandomNumber = 134123;
            mockRandomProvider.Setup((mock) => mock.Next()).Returns(expectedRandomNumber);
            RNG.Random = mockRandomProvider.Object;

            int actualRandomNumber = RNG.Next();

            Assert.AreEqual(expectedRandomNumber, actualRandomNumber);
            mockRandomProvider.Verify((mock) => mock.Next(), Times.Once);
        }
    }
}
