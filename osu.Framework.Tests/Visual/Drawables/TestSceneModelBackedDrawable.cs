// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Threading;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Drawables
{
    public class TestSceneModelBackedDrawable : FrameworkTestScene
    {
        private TestModelBackedDrawable backedDrawable;

        private void createModelBackedDrawable(bool hasIntermediate, bool showNullModel = false) =>
            Child = backedDrawable = new TestModelBackedDrawable
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Size = new Vector2(200),
                HasIntermediate = hasIntermediate,
                ShowNullModel = showNullModel
            };

        [Test]
        public void TestEmptyDefaultState()
        {
            AddStep("setup", () => createModelBackedDrawable(false));
            AddAssert("nothing shown", () => backedDrawable.DisplayedDrawable == null);
        }

        [Test]
        public void TestModelDefaultState()
        {
            TestDrawableModel drawableModel = null;

            AddStep("setup", () =>
            {
                createModelBackedDrawable(false);
                backedDrawable.Model = new TestModel(drawableModel = new TestDrawableModel(1).With(d => d.AllowLoad.Set()));
            });

            assertDrawableVisibility(1, () => drawableModel);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TestChangeModel(bool hasIntermediate)
        {
            TestDrawableModel firstModel = null;
            TestDrawableModel secondModel = null;

            AddStep("setup", () =>
            {
                createModelBackedDrawable(hasIntermediate);
                backedDrawable.Model = new TestModel(firstModel = new TestDrawableModel(1).With(d => d.AllowLoad.Set()));
            });

            assertDrawableVisibility(1, () => firstModel);

            AddStep("set second model", () => backedDrawable.Model = new TestModel(secondModel = new TestDrawableModel(2)));
            assertIntermediateVisibility(hasIntermediate, () => firstModel);

            AddStep("allow second model to load", () => secondModel.AllowLoad.Set());
            assertDrawableVisibility(2, () => secondModel);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TestChangeModelDuringLoad(bool hasIntermediate)
        {
            TestDrawableModel firstModel = null;
            TestDrawableModel secondModel = null;
            TestDrawableModel thirdModel = null;

            AddStep("setup", () =>
            {
                createModelBackedDrawable(hasIntermediate);
                backedDrawable.Model = new TestModel(firstModel = new TestDrawableModel(1).With(d => d.AllowLoad.Set()));
            });

            assertDrawableVisibility(1, () => firstModel);

            AddStep("set second model", () => backedDrawable.Model = new TestModel(secondModel = new TestDrawableModel(2)));
            assertIntermediateVisibility(hasIntermediate, () => firstModel);

            AddStep("set third model", () => backedDrawable.Model = new TestModel(thirdModel = new TestDrawableModel(3)));
            assertIntermediateVisibility(hasIntermediate, () => firstModel);

            AddStep("allow second model to load", () => secondModel.AllowLoad.Set());
            assertIntermediateVisibility(hasIntermediate, () => firstModel);

            AddStep("allow third model to load", () => thirdModel.AllowLoad.Set());
            assertDrawableVisibility(3, () => thirdModel);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TestOutOfOrderLoad(bool hasIntermediate)
        {
            TestDrawableModel firstModel = null;
            TestDrawableModel secondModel = null;

            AddStep("setup", () =>
            {
                createModelBackedDrawable(hasIntermediate);
                backedDrawable.Model = new TestModel(firstModel = new TestDrawableModel(1).With(d => d.AllowLoad.Set()));
            });

            assertDrawableVisibility(1, () => firstModel);

            AddStep("set second model", () => backedDrawable.Model = new TestModel(secondModel = new TestDrawableModel(2)));
            assertIntermediateVisibility(hasIntermediate, () => firstModel);

            AddStep("allow second model to load", () => secondModel.AllowLoad.Set());
            assertDrawableVisibility(2, () => secondModel);

            AddStep("allow first model to load", () => firstModel.AllowLoad.Set());
            assertDrawableVisibility(2, () => secondModel);
        }

        [Test]
        public void TestSetNullModel()
        {
            TestDrawableModel drawableModel = null;

            AddStep("setup", () =>
            {
                createModelBackedDrawable(false, true);
                backedDrawable.Model = new TestModel(drawableModel = new TestDrawableModel(1).With(d => d.AllowLoad.Set()));
            });

            assertDrawableVisibility(1, () => drawableModel);

            AddStep("set null model", () => backedDrawable.Model = null);
            AddUntilStep("null model shown", () => backedDrawable.DisplayedDrawable is TestNullDrawableModel);
        }

        [Test]
        public void TestInsideBufferedContainer()
        {
            TestDrawableModel drawableModel = null;

            AddStep("setup", () =>
            {
                Child = new BufferedContainer
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Size = new Vector2(200),
                    Child = backedDrawable = new TestModelBackedDrawable
                    {
                        RelativeSizeAxes = Axes.Both,
                        HasIntermediate = false,
                        ShowNullModel = false,
                        Model = new TestModel(drawableModel = new TestDrawableModel(1).With(d => d.AllowLoad.Set()))
                    }
                };
            });

            assertDrawableVisibility(1, () => drawableModel);
        }

        private void assertIntermediateVisibility(bool hasIntermediate, Func<Drawable> getLastFunc)
        {
            if (hasIntermediate)
                AddAssert("no drawable visible", () => backedDrawable.DisplayedDrawable == null);
            else
                AddUntilStep("last drawable visible", () => backedDrawable.DisplayedDrawable == getLastFunc());
        }

        private void assertDrawableVisibility(int id, Func<Drawable> getFunc)
        {
            AddUntilStep($"model {id} visible", () => backedDrawable.DisplayedDrawable == getFunc());
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
            private readonly int id;

            public readonly ManualResetEventSlim AllowLoad = new ManualResetEventSlim(false);

            protected virtual Color4 BackgroundColour
            {
                get
                {
                    switch (id % 5)
                    {
                        default:
                            return Color4.SkyBlue;

                        case 1:
                            return Color4.Tomato;

                        case 2:
                            return Color4.DarkGreen;

                        case 3:
                            return Color4.MediumPurple;

                        case 4:
                            return Color4.DarkOrchid;
                    }
                }
            }

            public TestDrawableModel(int id)
            {
                this.id = id;

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
                        Text = id > 0 ? $"model {id}" : "null"
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

        private class TestNullDrawableModel : TestDrawableModel
        {
            protected override Color4 BackgroundColour => Color4.SlateGray;

            public TestNullDrawableModel()
                : base(0)
            {
                AllowLoad.Set();
            }
        }

        private class TestModelBackedDrawable : ModelBackedDrawable<TestModel>
        {
            public bool ShowNullModel;

            public bool HasIntermediate;

            protected override Drawable CreateDrawable(TestModel model)
            {
                if (model == null && ShowNullModel)
                    return new TestNullDrawableModel();

                return model?.DrawableModel;
            }

            public new Drawable DisplayedDrawable => base.DisplayedDrawable;

            public new TestModel Model
            {
                set => base.Model = value;
            }

            protected override bool TransformImmediately => HasIntermediate;
        }
    }
}
