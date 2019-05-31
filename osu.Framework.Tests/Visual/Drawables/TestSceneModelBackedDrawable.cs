// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Threading;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.MathUtils;
using osu.Framework.Testing;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Drawables
{
    public class TestSceneModelBackedDrawable : TestScene
    {
        private TestModelBackedDrawable backedDrawable;

        private void createModelBackedDrawable(bool withPlaceholder, bool fadeOutImmediately) =>
            Child = backedDrawable = new TestModelBackedDrawable
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Size = new Vector2(200),
                InternalTransformImmediately = fadeOutImmediately,
                HasPlaceholder = withPlaceholder
            };

        [Test]
        public void TestEmptyDefaultState()
        {
            AddStep("setup", () => createModelBackedDrawable(false, false));

            assertDrawableVisibility(1, () => backedDrawable.DisplayedDrawable, false);
            assertPlaceholderVisibility(false);
        }

        [Test]
        public void TestPlaceholderDefaultState()
        {
            AddStep("setup", () => createModelBackedDrawable(true, false));

            assertDrawableVisibility(1, () => backedDrawable.DisplayedDrawable, false);
            assertPlaceholderVisibility(true);
        }

        [Test]
        public void TestModelDefaultState()
        {
            TestDrawableModel drawableModel = null;

            AddStep("setup", () =>
            {
                createModelBackedDrawable(false, false);
                backedDrawable.Model = new TestModel(drawableModel = new TestDrawableModel(1).With(d => d.AllowLoad.Set()));
            });

            assertDrawableVisibility(1, () => drawableModel, true);
            assertPlaceholderVisibility(false);
        }

        [Test]
        public void TestModelAndPlaceholderDefaultState()
        {
            TestDrawableModel drawableModel = null;

            AddStep("setup", () =>
            {
                createModelBackedDrawable(true, false);
                backedDrawable.Model = new TestModel(drawableModel = new TestDrawableModel(1).With(d => d.AllowLoad.Set()));
            });

            assertPlaceholderVisibility(true);
            assertDrawableVisibility(1, () => drawableModel, true);
            assertPlaceholderVisibility(false);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TestChangeModel(bool intermediatePlaceholder)
        {
            TestDrawableModel firstModel = null;
            TestDrawableModel secondModel = null;

            AddStep("setup", () =>
            {
                createModelBackedDrawable(intermediatePlaceholder, intermediatePlaceholder);
                backedDrawable.Model = new TestModel(firstModel = new TestDrawableModel(1).With(d => d.AllowLoad.Set()));
            });

            assertPlaceholderVisibility(false);
            AddStep("set second model", () => backedDrawable.Model = new TestModel(secondModel = new TestDrawableModel(2)));

            if (intermediatePlaceholder)
            {
                assertDrawableVisibility(1, () => firstModel, false);
                assertPlaceholderVisibility(true);
            }
            else
                assertDrawableVisibility(1, () => firstModel, true);

            AddStep("allow second model to load", () => secondModel.AllowLoad.Set());
            assertPlaceholderVisibility(false);
            assertDrawableVisibility(1, () => firstModel, false);
            assertDrawableVisibility(2, () => secondModel, true);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TestChangeModelDuringLoad(bool intermediatePlaceholder)
        {
            TestDrawableModel firstModel = null;
            TestDrawableModel secondModel = null;
            TestDrawableModel thirdModel = null;

            AddStep("setup", () =>
            {
                createModelBackedDrawable(intermediatePlaceholder, intermediatePlaceholder);
                backedDrawable.Model = new TestModel(firstModel = new TestDrawableModel(1).With(d => d.AllowLoad.Set()));
            });

            assertPlaceholderVisibility(false);

            AddStep("set second model", () => backedDrawable.Model = new TestModel(secondModel = new TestDrawableModel(2)));
            AddStep("set third model", () => backedDrawable.Model = new TestModel(thirdModel = new TestDrawableModel(3)));
            assertPlaceholderVisibility(intermediatePlaceholder);
            assertDrawableVisibility(1, () => firstModel, !intermediatePlaceholder);
            assertDrawableVisibility(2, () => secondModel, false);
            assertDrawableVisibility(3, () => thirdModel, false);

            AddStep("allow second model to load", () => secondModel.AllowLoad.Set());
            assertPlaceholderVisibility(intermediatePlaceholder);
            assertDrawableVisibility(1, () => firstModel, !intermediatePlaceholder);
            assertDrawableVisibility(2, () => secondModel, false);
            assertDrawableVisibility(3, () => thirdModel, false);

            AddStep("allow third model to load", () => thirdModel.AllowLoad.Set());
            assertPlaceholderVisibility(false);
            assertDrawableVisibility(1, () => firstModel, false);
            assertDrawableVisibility(2, () => secondModel, false);
            assertDrawableVisibility(3, () => thirdModel, true);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TestOutOfOrderLoad(bool intermediatePlaceholder)
        {
            TestDrawableModel firstModel = null;
            TestDrawableModel secondModel = null;

            AddStep("setup", () =>
            {
                createModelBackedDrawable(intermediatePlaceholder, intermediatePlaceholder);
                backedDrawable.Model = new TestModel(firstModel = new TestDrawableModel(1));
            });

            assertPlaceholderVisibility(intermediatePlaceholder);

            AddStep("set second model", () => backedDrawable.Model = new TestModel(secondModel = new TestDrawableModel(2)));
            assertPlaceholderVisibility(intermediatePlaceholder);
            assertDrawableVisibility(1, () => firstModel, false);
            assertDrawableVisibility(2, () => secondModel, false);

            AddStep("allow second model to load", () => secondModel.AllowLoad.Set());
            assertPlaceholderVisibility(false);
            assertDrawableVisibility(1, () => firstModel, false);
            assertDrawableVisibility(2, () => secondModel, true);

            AddStep("allow first model to load", () => firstModel.AllowLoad.Set());
            assertPlaceholderVisibility(false);
            assertDrawableVisibility(1, () => firstModel, false);
            assertDrawableVisibility(2, () => secondModel, true);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TestSetNullModel(bool withPlaceholder)
        {
            TestDrawableModel drawableModel = null;

            AddStep("setup", () =>
            {
                createModelBackedDrawable(withPlaceholder, false);
                backedDrawable.Model = new TestModel(drawableModel = new TestDrawableModel(1).With(d => d.AllowLoad.Set()));
            });

            assertDrawableVisibility(1, () => drawableModel, true);
            assertPlaceholderVisibility(false);

            AddStep("set null model", () => backedDrawable.Model = null);
            assertDrawableVisibility(1, () => drawableModel, false);
            assertPlaceholderVisibility(withPlaceholder);
        }

        private void assertDrawableVisibility(int id, Func<Drawable> drawable, bool visible)
        {
            if (visible)
                AddUntilStep($"drawable {id} visible", () => drawable().Parent != null && Precision.AlmostEquals(drawable().Alpha, 1));
            else
                AddUntilStep($"drawable {id} not visible", () => drawable() == null || drawable().Parent == null || Precision.AlmostEquals(drawable().Alpha, 0));
        }

        private void assertPlaceholderVisibility(bool visible)
        {
            if (visible)
                AddUntilStep("placeholder visible", () => Precision.AlmostEquals(backedDrawable.PlaceholderDrawable.Alpha, 1));
            else
                AddUntilStep("placeholder not visible", () => backedDrawable.PlaceholderDrawable == null || Precision.AlmostEquals(backedDrawable.PlaceholderDrawable.Alpha, 0));
        }

        private class TestModel
        {
            public readonly TestDrawableModel DrawableModel;

            public TestModel(TestDrawableModel drawableModel)
            {
                DrawableModel = drawableModel;
            }
        }

        private class TestDrawableModel : CompositeDrawable
        {
            public readonly ManualResetEventSlim AllowLoad = new ManualResetEventSlim(false);

            protected virtual Color4 BackgroundColour => Color4.SkyBlue;

            public TestDrawableModel(int id)
                : this($"Model {id}")
            {
            }

            protected TestDrawableModel(string text)
            {
                RelativeSizeAxes = Axes.Both;

                InternalChildren = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = BackgroundColour
                    },
                    new SpriteText
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Text = text
                    }
                };
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                if (!AllowLoad.Wait(TimeSpan.FromSeconds(10)))
                {
                }
            }
        }

        private class TestPlaceholder : TestDrawableModel
        {
            protected override Color4 BackgroundColour => Color4.SlateGray;

            public TestPlaceholder()
                : base("Placeholder")
            {
                AllowLoad.Set();
            }
        }

        private class TestModelBackedDrawable : ModelBackedDrawable<TestModel>
        {
            protected override Drawable CreateDrawable(TestModel model) => model?.DrawableModel;

            protected override Drawable CreatePlaceholder() => HasPlaceholder ? new TestPlaceholder() : null;

            public Drawable PlaceholderDrawable => HasPlaceholder ? (((DelayedLoadWrapper)InternalChildren[0]).Content as TestPlaceholder)?.Parent : null;

            public new Drawable DisplayedDrawable => base.DisplayedDrawable?.Parent;

            public new TestModel Model
            {
                get => base.Model;
                set => base.Model = value;
            }

            public bool InternalTransformImmediately;

            public bool HasPlaceholder;

            protected override bool TransformImmediately => InternalTransformImmediately;
        }
    }
}
