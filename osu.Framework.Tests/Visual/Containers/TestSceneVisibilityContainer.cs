// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Containers
{
    public class TestSceneVisibilityContainer : FrameworkTestScene
    {
        private TestVisibilityContainer testContainer;

        [Test]
        public void TestShowHide()
        {
            Steps.AddStep("create container", () => Child = testContainer = new TestVisibilityContainer());

            checkHidden(true);

            Steps.AddStep("show", () => testContainer.Show());
            checkVisible();

            Steps.AddStep("hide", () => testContainer.Hide());
            checkHidden();

            Steps.AddAssert("fire count is 2", () => testContainer.FireCount == 2);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void TestStartHidden(bool startHidden)
        {
            Steps.AddStep("create container", () => Child = testContainer =
                new TestVisibilityContainer(startHidden) { State = { Value = Visibility.Visible } });

            checkVisible(!startHidden);

            Steps.AddStep("hide", () => testContainer.Hide());
            checkHidden();

            Steps.AddAssert("fire count is 2", () => testContainer.FireCount == 2);
        }

        private void checkHidden(bool instant = false)
        {
            Steps.AddAssert("is hidden", () => testContainer.State.Value == Visibility.Hidden);
            if (instant)
                Steps.AddAssert("alpha zero", () => testContainer.Alpha == 0);
            else
                Steps.AddUntilStep("alpha zero", () => testContainer.Alpha == 0);
        }

        private void checkVisible(bool instant = false)
        {
            Steps.AddAssert("is visible", () => testContainer.State.Value == Visibility.Visible);
            if (instant)
                Steps.AddAssert("alpha one", () => testContainer.Alpha == 1);
            else
                Steps.AddUntilStep("alpha one", () => testContainer.Alpha == 1);
        }

        private class TestVisibilityContainer : VisibilityContainer
        {
            private readonly bool startHidden;

            protected override bool StartHidden => startHidden;

            public TestVisibilityContainer(bool startHidden = true)
            {
                this.startHidden = startHidden;

                Size = new Vector2(0.5f);
                RelativeSizeAxes = Axes.Both;

                Anchor = Anchor.Centre;
                Origin = Anchor.Centre;

                Children = new Drawable[]
                {
                    new Box
                    {
                        Colour = Color4.Cyan,
                        RelativeSizeAxes = Axes.Both,
                    },
                };

                State.ValueChanged += e => FireCount++;
            }

            public int FireCount { get; private set; }

            protected override void PopIn()
            {
                this.FadeIn(1000, Easing.OutQuint);
                this.ScaleTo(1, 1000, Easing.OutElastic);
            }

            protected override void PopOut()
            {
                this.FadeOut(1000, Easing.OutQuint);
                this.ScaleTo(0.4f, 1000, Easing.OutQuint);
            }
        }
    }
}
