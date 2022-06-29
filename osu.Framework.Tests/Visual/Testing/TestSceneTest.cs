// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Linq;
using NUnit.Framework;
using osu.Framework.Development;
using osu.Framework.Testing;
using osu.Framework.Testing.Drawables.Steps;

namespace osu.Framework.Tests.Visual.Testing
{
    public class TestSceneTest : FrameworkTestScene
    {
        private int setupRun;
        private int setupStepsRun;
        private int setupStepsDummyRun;
        private int teardownStepsRun;
        private int teardownStepsDummyRun;
        private int testRunCount;
        private int testRunCountDummyRun;

        [SetUp]
        public virtual void SetUp()
        {
            // Under nUnit, [SetUp] is run once for the base TestScene.TestConstructor() method. Our own test method has not run yet by this point, so this is ignored.
            // The test browser does _not_ invoke [SetUp] for the constructor, and the TestScene.TestConstructor() method is skipped.
            if (DebugUtils.IsNUnitRunning && TestContext.CurrentContext.Test.MethodName == nameof(TestConstructor))
                return;

            setupRun++;
        }

        [SetUpSteps]
        public virtual void SetUpSteps()
        {
            // Under nUnit, [SetUpSteps] is run once for the base TestScene.TestConstructor() method. Our own test method has not run yet by this point, so this is ignored.
            // The test browser does _not_ invoke [SetUpSteps] for the constructor, and the TestScene.TestConstructor() method is skipped.
            if (DebugUtils.IsNUnitRunning && TestContext.CurrentContext.Test.MethodName == nameof(TestConstructor))
                return;

            AddStep(new SingleStepButton(true)
            {
                Name = "set up dummy",
                Action = () => setupStepsDummyRun++
            });

            AddStep("set up second step", () => { });
            setupStepsRun++;
        }

        [TearDownSteps]
        public virtual void TearDownSteps()
        {
            // Under nUnit, [TearDownSteps] is run once for the base TestScene.TestConstructor() method. Our own test method has not run yet by this point, so this is ignored.
            // The test browser does _not_ invoke [TearDownSteps] for the constructor, and the TestScene.TestConstructor() method is skipped.
            if (DebugUtils.IsNUnitRunning && TestContext.CurrentContext.Test.MethodName == nameof(TestConstructor))
                return;

            AddStep("tear down dummy", () => teardownStepsDummyRun++);
            teardownStepsRun++;
        }

        public TestSceneTest()
        {
            // A dummy test used to pad the setup steps for visual testing purposes.
            AddStep("dummy test", () => { });
        }

        [Test, Repeat(2)]
        public void TestTest()
        {
            AddStep("increment run count", () => testRunCountDummyRun++);

            // Under nUnit, the test steps for each method are run during [TearDown], meaning setup count == test run count.
            // The test browser immediately invokes all test methods _before_ any test steps are run, meaning test run count == total number of tests,
            // so the dummy value is used instead to track the true test run count inside the test browser.
            //
            // nUnit:
            // [SetUp] -> [Test] -> [TearDown] -> {{ Run steps }} -> [SetUp] -> [Test] -> [TearDown] -> {{ Run steps }} ...
            // Test browser:
            // [SetUp] -> [Test] -> [TearDown] -> [SetUp] -> [Test] -> [TearDown] -> ... -> {{ Run steps }}
            //
            AddAssert("correct [SetUp] run count", () => setupRun == (DebugUtils.IsNUnitRunning ? testRunCount : testRunCountDummyRun));

            // Under both nUnit and the test browser, this should be invoked once for all test methods _before_ any test steps are run.
            AddAssert("correct [SetUpSteps] run count", () => setupStepsRun == testRunCount);

            // Under both nUnit and the test browser, this should be invoked once before each test method.
            AddAssert("correct setup step run", () => setupStepsDummyRun == testRunCountDummyRun);

            // Under both nUnit and the test browser, this should be invoked once for all test methods _before_ any test steps are run.
            AddAssert("correct [TearDownSteps] run count", () => teardownStepsRun == testRunCount);

            // Under both nUnit and the test browser, this should be invoked once _after_ each test method.
            AddAssert("correct teardown step run", () => teardownStepsDummyRun == testRunCountDummyRun - 1);

            AddAssert("setup step marked as such", () => StepsContainer.OfType<StepButton>().First(s => s.Text.ToString() == "set up second step").IsSetupStep);

            testRunCount++;
        }

        [TestCase(1)]
        [TestCase(2)]
        [Repeat(2)]
        public void TestTestCase(int _) => TestTest();

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
