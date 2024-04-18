// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using NUnit.Framework.Internal;
using osu.Framework.Testing;

namespace osu.Framework.Tests.Visual.Testing
{
    [HeadlessTest]
    public partial class TestSceneTestRetry : FrameworkTestScene
    {
        private int runCount;
        private string? currentTest;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            if (FrameworkEnvironment.FailFlakyTests)
                Assert.Ignore("Can't run while failing flaky tests.");
        }

        [SetUp]
        public void Setup() => Schedule(() =>
        {
            if (currentTest == TestExecutionContext.CurrentContext.CurrentTest.Name)
                return;

            runCount = 0;
            currentTest = TestExecutionContext.CurrentContext.CurrentTest.Name;
        });

        [Test]
        [FlakyTest(10)]
        public void FlakyTestWithAssert()
        {
            AddStep("increment", () => runCount++);
            AddAssert("assert if not ran 5 times", () => runCount, () => Is.EqualTo(5));
        }

        [Test]
        [FlakyTest(3)]
        public void FlakyTestWithUntilStep()
        {
            AddStep("increment", () => runCount++);
            AddUntilStep("assert if not ran 2 times", () => runCount, () => Is.EqualTo(2));
        }
    }
}
