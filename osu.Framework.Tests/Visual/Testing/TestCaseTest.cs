// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Testing;

namespace osu.Framework.Tests.Visual.Testing
{
    public class TestCaseTest : TestCase
    {
        private int setupRun;
        private int setupStepsRun;
        private int testRunCount;

        [SetUp]
        public void SetUp()
        {
            setupRun++;
        }

        [SetUpSteps]
        public void SetUpSteps()
        {
            setupStepsRun++;
        }

        public TestCaseTest()
        {
            // run before setup
            testRunCount++;
        }

        [Test]
        public void Test1()
        {
            testRunCount++;
            Assert.AreEqual(testRunCount, setupRun);
            Assert.AreEqual(testRunCount, setupStepsRun);
        }

        [Test]
        public void Test2()
        {
            testRunCount++;
            Assert.AreEqual(testRunCount, setupRun);
            Assert.AreEqual(testRunCount, setupStepsRun);
        }
    }
}
