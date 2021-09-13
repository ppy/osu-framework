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
            AddUntilStep("game exited", () => game.Parent == null);
        }

        [Test]
        public void TestMultipleNestedGames()
        {
            TestGame game = null;
            TestGame game2 = null;

            AddStep("Add game", () => AddGame(game = new TestGame()));
            AddUntilStep("game not exited", () => game.Parent != null);

            AddStep("Add game 2", () => AddGame(game2 = new TestGame()));
            AddUntilStep("game exited", () => game.Parent == null);
            AddUntilStep("game2 not exited", () => game2.Parent != null);

            AddStep("exit game2", () => game2.Exit());
            AddUntilStep("game2 exited", () => game2.Parent == null);
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
