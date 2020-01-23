// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Development;
using osu.Framework.Testing;

namespace osu.Framework.Tests.Visual.Testing
{
    public class TestSceneTest : FrameworkTestScene
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

        public TestSceneTest()
        {
            Schedule(() =>
            {
                // [SetUp] gets run via TestConstructor() when we are running under nUnit.
                // note that in TestBrowser's case, this does not invoke SetUp methods, so we skip this increment.
                // schedule is required to ensure that IsNUnitRunning is initialised.
                if (DebugUtils.IsNUnitRunning)
                    testRunCount++;
            });

            AddStep("dummy step", () => { });
        }

        [Test, Repeat(2)]
        public void Test()
        {
            AddStep("increment run count", () => testRunCount++);
            AddAssert("correct setup run count", () => testRunCount == setupRun);
            AddAssert("correct setup steps run count", () => (DebugUtils.IsNUnitRunning ? testRunCount : 2) == setupStepsRun);
        }

        protected override ITestSceneTestRunner CreateRunner() => new TestRunner();

        private class TestRunner : TestSceneTestRunner
        {
            public override void RunTestBlocking(TestScene test)
            {
                base.RunTestBlocking(test);

                // This will only ever trigger via NUnit
                Assert.That(test.StepsContainer, Has.Count.GreaterThan(0));
            }
        }
    }
}
