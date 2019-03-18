// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Screens;
using osu.Framework.Testing;

namespace osu.Framework.Tests.Visual.TestCaseUserInterface
{
    public class TestCaseScreenCarousel : TestCase
    {
        private TestScreenCarousel screenCarousel;

        [SetUp]
        public void Setup()
        {
            Schedule(() =>
            {
                Child = screenCarousel = new TestScreenCarousel { RelativeSizeAxes = Axes.Both };
                screenCarousel.AddScreen(TestScreens.BaseScreen, new TestCaseScreenStack.TestScreen(showButtons: false));
                screenCarousel.AddScreen(TestScreens.TestScreen1, new TestCaseScreenStack.TestScreen(showButtons: false));
                screenCarousel.AddScreen(TestScreens.TestScreen2, new TestCaseScreenStack.TestScreen(showButtons: false));
            });

        }

        [Test]
        public void SwitchToNewTest()
        {
            AddStep("Switch to screen 1", () => screenCarousel.SwitchTo(TestScreens.TestScreen1));
            AddAssert("base is current screen", () => screenCarousel.CurrentIndex == TestScreens.TestScreen1);
        }

        [Test]
        public void MultiScreenTraversalTest()
        {
            AddStep("Switch to base screen", () => screenCarousel.SwitchTo(TestScreens.BaseScreen));
            AddStep("Switch to screen1", () => screenCarousel.SwitchTo(TestScreens.TestScreen1));
            AddStep("Switch to screen2", () => screenCarousel.SwitchTo(TestScreens.TestScreen2));
            AddStep("Switch to base screen", () => screenCarousel.SwitchTo(TestScreens.BaseScreen));
            AddAssert("base is current screen", () => screenCarousel.CurrentIndex == TestScreens.BaseScreen);
        }

        [Test]
        public void SameScreenTest()
        {
            AddStep("Switch to base screen", () => screenCarousel.SwitchTo(TestScreens.BaseScreen));
            AddStep("Switch to base screen", () => screenCarousel.SwitchTo(TestScreens.BaseScreen));
            AddAssert("base is current screen", () => screenCarousel.CurrentIndex == TestScreens.BaseScreen);
        }

        private class TestScreenCarousel : ScreenCarousel<TestScreens>
        {
            public TestScreens CurrentIndex => Screens.FirstOrDefault(x => x.Value == CurrentScreen).Key;
        }

        private enum TestScreens
        {
            BaseScreen,
            TestScreen1,
            TestScreen2
        }
    }
}
