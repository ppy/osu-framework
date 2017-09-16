// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using Moq;
using NUnit.Framework;
using osu.Framework.MathUtils;

namespace osu.Framework.Tests.MathUtils
{
    [TestFixture]
    public class TestRNG
    {
        [Test]
        public void TestNext()
        {
            Mock<IRandomProvider> mockRandomProvider = new Mock<IRandomProvider>();
            const int expected_random_number = 134123;
            mockRandomProvider.Setup(mock => mock.Next()).Returns(expected_random_number);
            RNG.Random = mockRandomProvider.Object;

            int actualRandomNumber = RNG.Next();

            Assert.AreEqual(expected_random_number, actualRandomNumber);
            mockRandomProvider.Verify(mock => mock.Next(), Times.Once);
        }
    }
}
