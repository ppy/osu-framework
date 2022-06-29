// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Linq;
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
            AddStep("create container", () => Child = testContainer = new TestVisibilityContainer());

            checkHidden(true);

            AddStep("show", () => testContainer.Show());
            checkVisible();

            AddStep("hide", () => testContainer.Hide());
            checkHidden();

            AddAssert("fire count is 2", () => testContainer.FireCount == 2);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void TestStartHidden(bool startHidden)
        {
            AddStep("create container", () => Child = testContainer =
                new TestVisibilityContainer(startHidden) { State = { Value = Visibility.Visible } });

            checkVisible(!startHidden);

            AddStep("hide", () => testContainer.Hide());
            checkHidden();

            AddAssert("fire count is 2", () => testContainer.FireCount == 2);
        }

        [Test]
        public void TestNotStartHiddenButHidden()
        {
            AddStep("create container", () => Child = testContainer =
                new TestVisibilityContainer(false) { State = { Value = Visibility.Hidden } });

            AddAssert("alpha above zero", () => testContainer.Alpha > 0);

            checkHidden(false);
        }

        [Test]
        public void TestShowInCtor()
        {
            AddStep("create container", () =>
            {
                var container = new TestVisibilityContainer(null);
                container.Show();

                var containingContainer = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Child = testContainer = container
                };

                containingContainer.OnLoadComplete += _ => testContainer.Hide();

                Child = containingContainer;
            });

            checkHidden();
        }

        [TestCase(true, true)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(false, false)]
        public void TestStartHiddenNested(bool startHidden, bool immediatelyVisible)
        {
            TestNestedVisibilityContainer visibility = null;

            AddStep("create container", () =>
            {
                Child = testContainer =
                    visibility = new TestNestedVisibilityContainer(startHidden) { State = { Value = immediatelyVisible ? Visibility.Visible : Visibility.Hidden } };

                if (!immediatelyVisible) testContainer.Show();
            });

            checkVisible(!startHidden);

            AddAssert("box has transforms", () => visibility.BoxHasTransforms);
            AddStep("hide", () => testContainer.Hide());

            checkHidden();
            AddAssert("box doesn't have transforms", () => !visibility.BoxHasTransforms);

            AddAssert("fire count is 2", () => testContainer.FireCount == 2);
        }

        private void checkHidden(bool instant = false)
        {
            AddAssert("is hidden", () => testContainer.State.Value == Visibility.Hidden);
            if (instant)
                AddAssert("alpha zero", () => testContainer.Alpha == 0);
            else
                AddUntilStep("alpha zero", () => testContainer.Alpha == 0);
        }

        private void checkVisible(bool instant = false)
        {
            AddAssert("is visible", () => testContainer.State.Value == Visibility.Visible);
            if (instant)
                AddAssert("alpha one", () => testContainer.Alpha == 1);
            else
                AddUntilStep("wait alpha one", () => testContainer.Alpha == 1);
        }

        private class TestNestedVisibilityContainer : TestVisibilityContainer
        {
            public bool BoxHasTransforms => box.Transforms.Any();

            private readonly TestVisibilityContainer nested;
            private readonly Box box;

            public TestNestedVisibilityContainer(bool startHidden = true)
                : base(startHidden)
            {
                Add(nested = new TestVisibilityContainer(true, Color4.Yellow));

                nested.Add(box = new Box
                {
                    Colour = Color4.Black,
                    RelativeSizeAxes = Axes.Both,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Size = new Vector2(0.5f),
                });
            }

            protected override void PopIn()
            {
                base.PopIn();
                nested.Show();
                box.RotateTo(360, 5000);
            }

            protected override void PopOut()
            {
                base.PopOut();
                nested.Hide();
                box.RotateTo(0);
            }
        }

        private class TestVisibilityContainer : VisibilityContainer
        {
            private readonly bool? startHidden;

            protected override bool StartHidden => startHidden ?? base.StartHidden;

            public TestVisibilityContainer(bool? startHidden = true, Color4? colour = null)
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
                        Colour = colour ?? Color4.Cyan,
                        RelativeSizeAxes = Axes.Both,
                    },
                };

                State.ValueChanged += _ => FireCount++;
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
