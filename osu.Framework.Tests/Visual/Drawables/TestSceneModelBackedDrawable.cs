// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Testing;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Drawables
{
    public class TestSceneModelBackedDrawable : TestScene
    {
        private TestModelBackedDrawable2 backedDrawable;

        [SetUp]
        public void Setup() => Schedule(() =>
        {
            Child = backedDrawable = new TestModelBackedDrawable2
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Size = new Vector2(200)
            };
        });

        [TestCase(false)]
        [TestCase(true)]
        public void TestDefaultState(bool withPlaceholder)
        {
            // Need a local MDB, otherwise the fade out immediately flag is not set prior to load
            AddStep("setup", () =>
            {
                Child = backedDrawable = new TestModelBackedDrawable2
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Size = new Vector2(200),
                    InternalFadeOutImmediately = withPlaceholder
                };
            });

            if (withPlaceholder)
                AddAssert("placeholder displayed", () => backedDrawable.DisplayedDrawable is TestPlaceholder);
            else
                AddAssert("no drawable displayed", () => backedDrawable.DisplayedDrawable == null);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TestSingleDelayedLoad(bool withPlaceholder)
        {
            TestDrawableModel2 drawableModel = null;

            AddStep("setup", () => backedDrawable.InternalFadeOutImmediately = withPlaceholder);
            AddStep("set model", () => backedDrawable.Model = new TestModel2(drawableModel = new TestDrawableModel2(1)));

            if (withPlaceholder)
                AddAssert("placeholder displayed", () => backedDrawable.DisplayedDrawable is TestPlaceholder);
            else
                AddAssert("no drawable displayed", () => backedDrawable.DisplayedDrawable == null);

            AddStep("allow load", () => drawableModel.AllowLoad.Set());
            AddUntilStep("wait for model to be loaded", () => drawableModel.IsLoaded);

            AddAssert("model displayed", () => backedDrawable.DisplayedDrawable == drawableModel);
        }

        [Test]
        public void TestMultipleLoadDisplaysSinglePlaceholder()
        {
            TestDrawableModel2 firstModel = null;
            TestDrawableModel2 secondModel = null;
            Drawable placeholder = null;

            AddStep("setup", () => backedDrawable.InternalFadeOutImmediately = true);
            AddStep("set first model", () =>
            {
                backedDrawable.Model = new TestModel2(firstModel = new TestDrawableModel2(1));
                placeholder = backedDrawable.DisplayedDrawable;
            });

            AddStep("set second model", () => backedDrawable.Model = new TestModel2(secondModel = new TestDrawableModel2(2)));
            AddAssert("first placeholder still displayed", () => backedDrawable.DisplayedDrawable == placeholder);

            AddStep("allow models to load", () =>
            {
                firstModel.AllowLoad.Set();
                secondModel.AllowLoad.Set();
            });
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TestSequentialDelayedLoad(bool withPlaceholder)
        {
            const int model_count = 3;

            var drawableModels = new List<TestDrawableModel2>(model_count);

            AddStep("setup", () =>
            {
                drawableModels.Clear();
                backedDrawable.InternalFadeOutImmediately = withPlaceholder;
            });

            for (int i = 0; i < model_count; i++)
            {
                int localI = i;
                AddStep($"set model {i + 1}", () =>
                {
                    var model = new TestDrawableModel2(localI + 1);
                    drawableModels.Add(model);

                    backedDrawable.Model = new TestModel2(model);
                });
            }

            // Due to potential left-over threading from elsewhere, we may have to wait for all models to get into a loading state
            AddUntilStep("all loading", () => drawableModels.TrueForAll(d => d.LoadState == LoadState.Loading));

            for (int i = 0; i < model_count - 1; i++)
            {
                int localI = i;
                AddStep($"allow model {i + 1} to load", () => drawableModels[localI].AllowLoad.Set());
                AddAssert("no model displayed", () => backedDrawable.DisplayedDrawable == null || backedDrawable.DisplayedDrawable is TestPlaceholder);
                AddWaitStep("wait for potential load", 5);
                AddAssert($"model {i + 1} not loaded", () => !drawableModels[localI].IsLoaded);
            }

            AddStep($"allow model {model_count} to load", () => drawableModels[model_count - 1].AllowLoad.Set());
            AddUntilStep($"model {model_count} not loaded", () => drawableModels[model_count - 1].IsLoaded);
            AddAssert($"model {model_count} displayed", () => backedDrawable.DisplayedDrawable == drawableModels[model_count - 1]);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TestOutOfOrderDelayedLoad(bool withPlaceholder)
        {
            const int model_count = 3;

            var drawableModels = new List<TestDrawableModel2>(model_count);

            AddStep("setup", () =>
            {
                drawableModels.Clear();
                backedDrawable.InternalFadeOutImmediately = withPlaceholder;
            });

            for (int i = 0; i < model_count; i++)
            {
                int localI = i;
                AddStep($"set model {i + 1}", () =>
                {
                    var model = new TestDrawableModel2(localI + 1);
                    drawableModels.Add(model);

                    backedDrawable.Model = new TestModel2(model);
                });
            }

            // Due to potential left-over threading from elsewhere, we may have to wait for all models to get into a loading state
            AddUntilStep("all loading", () => drawableModels.TrueForAll(d => d.LoadState == LoadState.Loading));

            AddStep($"allow model {model_count} to load", () => drawableModels[model_count - 1].AllowLoad.Set());
            AddAssert($"model {model_count} displayed", () => backedDrawable.DisplayedDrawable == drawableModels[model_count - 1]);

            for (int i = model_count - 2; i >= 0; i--)
            {
                int localI = i;
                AddStep($"allow model {i + 1} to load", () => drawableModels[localI].AllowLoad.Set());
                AddAssert($"model {model_count} still displayed", () => backedDrawable.DisplayedDrawable == drawableModels[model_count - 1]);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TestSetNullModel(bool withPlaceholder)
        {
            TestDrawableModel2 drawableModel = null;

            AddStep("setup", () => backedDrawable.InternalFadeOutImmediately = withPlaceholder);
            AddStep("set model", () =>
            {
                backedDrawable.Model = new TestModel2(drawableModel = new TestDrawableModel2(1));
                drawableModel.AllowLoad.Set();
            });

            AddUntilStep("model is displayed", () => backedDrawable.DisplayedDrawable == drawableModel);

            AddStep("set null model", () => backedDrawable.Model = null);

            if (withPlaceholder)
                AddAssert("placeholder displayed", () => backedDrawable.DisplayedDrawable is TestPlaceholder);
            else
                AddAssert("no drawable displayed", () => backedDrawable.DisplayedDrawable == null);
        }

        private class TestModel2
        {
            public readonly TestDrawableModel2 DrawableModel;

            public TestModel2(TestDrawableModel2 drawableModel)
            {
                DrawableModel = drawableModel;
            }
        }

        private class TestDrawableModel2 : CompositeDrawable
        {
            public readonly ManualResetEventSlim AllowLoad = new ManualResetEventSlim(false);

            public TestDrawableModel2(int id)
                : this($"Model {id}")
            {
            }

            protected TestDrawableModel2(string text)
            {
                RelativeSizeAxes = Axes.Both;

                InternalChildren = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.SkyBlue
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
                    throw new TimeoutException();
            }
        }

        private class TestPlaceholder : TestDrawableModel2
        {
            public TestPlaceholder()
                : base("Placeholder")
            {
                AllowLoad.Set();
            }
        }

        private class TestModelBackedDrawable2 : ModelBackedDrawable<TestModel2>
        {
            protected override Drawable CreateDrawable(TestModel2 model)
            {
                if (model == null)
                    return FadeOutImmediately ? new TestPlaceholder() : null;

                return model.DrawableModel;
            }

            public new Drawable DisplayedDrawable => base.DisplayedDrawable;

            public new TestModel2 Model
            {
                get => base.Model;
                set => base.Model = value;
            }

            public bool InternalFadeOutImmediately;

            protected override bool FadeOutImmediately => InternalFadeOutImmediately;
        }
    }
}
