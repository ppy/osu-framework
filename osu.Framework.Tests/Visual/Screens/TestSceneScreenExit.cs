// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Screens;
using osu.Framework.Testing;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Screens
{
    public class TestSceneScreenExit : FrameworkTestScene
    {
        private ScreenStack stack;

        [SetUpSteps]
        public void SetUpSteps()
        {
            AddStep("Create new screen stack", () => { Child = stack = new ScreenStack { RelativeSizeAxes = Axes.Both }; });
        }

        [Test]
        public void ScreenExitTest()
        {
            TestScreen beforeExit = null;

            AddStep("Push test screen", () => stack.Push(beforeExit = new TestScreen("BEFORE EXIT")));
            AddUntilStep("Wait for current", () => beforeExit.IsLoaded);

            AddStep("Exit test screen", () => beforeExit.Exit()); // No exceptions should be thrown.
            AddAssert("Test screen is not current", () => !ReferenceEquals(beforeExit, stack.CurrentScreen));
            AddAssert("Stack is not empty", () => stack.CurrentScreen != null);
        }

        private class TestScreen : Screen
        {
            private readonly string screenText;

            public TestScreen(string screenText)
            {
                this.screenText = screenText;
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                AddInternal(new SpriteText
                {
                    Text = screenText,
                    Colour = Color4.White,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                });
            }

            public override bool OnExiting(IScreen next)
            {
                this.Push(new TestScreen("AFTER EXIT"));
                return true;
            }
        }
    }
}
