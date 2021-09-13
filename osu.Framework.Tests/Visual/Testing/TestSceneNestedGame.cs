// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using NUnit.Framework;
using osu.Framework.Testing;

namespace osu.Framework.Tests.Visual.Testing
{
    public class TestSceneNestedGame : FrameworkTestScene
    {
        private bool hostWasRunningAfterNestedExit;

        [SetUpSteps]
        public void SetUpSteps()
        {
            AddStep("reset host running", () => hostWasRunningAfterNestedExit = false);
        }

        [Test]
        public void AddGameUsingStandardMethodThrows()
        {
            AddStep("Add game via add throws", () => Assert.Throws<InvalidOperationException>(() => Add(new TestGame())));
        }

        [Test]
        public void TestNestedGame()
        {
            TestGame game = null;

            AddStep("Add game", () => AddGame(game = new TestGame()));
            AddStep("exit game", () => game.Exit());
            AddUntilStep("game expired", () => game.Parent == null);
        }

        [TearDownSteps]
        public void TearDownSteps()
        {
            AddStep("mark host running", () => hostWasRunningAfterNestedExit = true);
        }

        protected override void RunTests()
        {
            base.RunTests();

            Assert.IsTrue(hostWasRunningAfterNestedExit);
        }
    }
}
