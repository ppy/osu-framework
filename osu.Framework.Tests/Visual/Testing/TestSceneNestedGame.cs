// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Testing;
using osuTK;
using osuTK.Graphics;

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
            NestedTestGame game = null;

            AddStep("Add game", () => AddGame(game = new NestedTestGame()));
            AddStep("exit game", () => game.Exit());
            AddUntilStep("game exited", () => game.Parent == null);
        }

        [Test]
        public void TestMultipleNestedGamesSequential()
        {
            NestedTestGame game = null;
            NestedTestGame game2 = null;

            AddStep("Add game", () => AddGame(game = new NestedTestGame()));
            AddUntilStep("game not exited", () => game.Parent != null);
            AddStep("exit game", () => game.Exit());

            AddStep("Add game 2", () => AddGame(game2 = new NestedTestGame()));
            AddUntilStep("game exited", () => game.Parent == null);
            AddUntilStep("game2 not exited", () => game2.Parent != null);

            AddStep("exit game2", () => game2.Exit());
            AddUntilStep("game2 exited", () => game2.Parent == null);
        }

        [Test]
        public void TestMultipleNestedGamesOverwrite()
        {
            NestedTestGame game = null;
            NestedTestGame game2 = null;

            AddStep("Add game", () => AddGame(game = new NestedTestGame()));
            AddUntilStep("game not exited", () => game.Parent != null);

            AddStep("Add game 2", () => AddGame(game2 = new NestedTestGame()));
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

        protected override void RunTestsFromNUnit()
        {
            base.RunTestsFromNUnit();

            Assert.IsTrue(hostWasRunningAfterNestedExit);
        }

        internal class NestedTestGame : TestGame
        {
            private Box box;

            [BackgroundDependencyLoader]
            private void load()
            {
                Add(box = new Box
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Size = new Vector2(150, 150),
                    Colour = Color4.Tomato
                });
            }

            protected override void Update()
            {
                base.Update();
                box.Rotation += (float)Time.Elapsed / 10;
            }

            protected override void Dispose(bool isDisposing)
            {
                if (IsDisposed)
                    throw new InvalidOperationException("Dispose called multiple times");

                base.Dispose(isDisposing);
            }
        }
    }
}
