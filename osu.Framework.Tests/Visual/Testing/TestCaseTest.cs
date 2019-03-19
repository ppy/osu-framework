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
            Schedule(() =>
            {
                // [SetUp] gets run via TestConstructor() when we are running under nUnit.
                // note that in TestBrowser's case, this does not invoke SetUp methods, so we skip this increment.
                // schedule is required to ensure that IsNUnitRunning is initialised.
                if (IsNUnitRunning)
                    testRunCount++;
            });
        }

        [Test]
        public void Test1()
        {
            AddStep("increment run count", () => testRunCount++);
            AddAssert("correct setup run count", () => testRunCount == setupRun);
            AddAssert("correct setup steps run count", () => (IsNUnitRunning ? testRunCount : 2) == setupStepsRun);
        }

        [Test]
        public void Test2()
        {
            AddStep("increment run count", () => testRunCount++);
            AddAssert("correct setup run count", () => testRunCount == setupRun);
            AddAssert("correct setup steps run count", () => (IsNUnitRunning ? testRunCount : 2) == setupStepsRun);
        }
    }
}
