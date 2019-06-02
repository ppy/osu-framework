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
        private TestModelBackedDrawable backedDrawable;

        private void createModelBackedDrawable(bool withPlaceholder, bool fadeOutImmediately) =>
            Child = backedDrawable = new TestModelBackedDrawable
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Size = new Vector2(200),
                InternalFadeOutImmediately = fadeOutImmediately,
                HasPlaceholder = withPlaceholder
            };

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void TestDefaultState(bool withPlaceholder, bool fadeOutImmediately)
        {
            AddStep("setup", () => createModelBackedDrawable(withPlaceholder, fadeOutImmediately));

            if (withPlaceholder)
                AddAssert("placeholder displayed", () => backedDrawable.DisplayedDrawable is TestPlaceholder);
            else
                AddAssert("no drawable displayed", () => backedDrawable.DisplayedDrawable == null);
        }

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void TestSingleDelayedLoad(bool withPlaceholder, bool fadeOutImmediately)
        {
            TestDrawableModel drawableModel = null;

            AddStep("setup", () => createModelBackedDrawable(withPlaceholder, fadeOutImmediately));

            AddStep("set model", () => backedDrawable.Model = new TestModel(drawableModel = new TestDrawableModel(1)));

            if (withPlaceholder)
                AddAssert("placeholder displayed", () => backedDrawable.DisplayedDrawable is TestPlaceholder);
            else
                AddAssert("no drawable displayed", () => backedDrawable.DisplayedDrawable == null);

            AddStep("allow load", () => drawableModel.AllowLoad.Set());
            AddUntilStep("model displayed", () => backedDrawable.DisplayedDrawable == drawableModel);
        }

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void TestMultipleLoadDisplaysSinglePlaceholder(bool withPlaceholder, bool fadeOutImmediately)
        {
            TestDrawableModel firstModel = null;
            TestDrawableModel secondModel = null;
            Drawable placeholder = null;

            AddStep("setup", () => createModelBackedDrawable(withPlaceholder, fadeOutImmediately));

            AddStep("set first model", () =>
            {
                backedDrawable.Model = new TestModel(firstModel = new TestDrawableModel(1));
                placeholder = backedDrawable.DisplayedDrawable;
            });

            AddStep("set second model", () => backedDrawable.Model = new TestModel(secondModel = new TestDrawableModel(2)));
            AddAssert("first placeholder still displayed", () => backedDrawable.DisplayedDrawable == placeholder);

            AddStep("allow first model to load", () => firstModel.AllowLoad.Set());
            AddAssert("first placeholder still displayed", () => backedDrawable.DisplayedDrawable == placeholder);

            AddStep("allow second model to load", () => secondModel.AllowLoad.Set());
            AddUntilStep("second model displayed", () => backedDrawable.DisplayedDrawable == secondModel);
        }

        /// <summary>
        /// Covers <see cref="ModelBackedDrawable{T}.FadeOutImmediately"/> usage.
        /// </summary>
        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void TestIntermediaryPlaceholder(bool withPlaceholder, bool fadeOutImmediately)
        {
            TestDrawableModel firstModel = null;
            TestDrawableModel secondModel = null;

            AddStep("setup", () => createModelBackedDrawable(withPlaceholder, fadeOutImmediately));

            AddStep("set first model", () => backedDrawable.Model = new TestModel(firstModel = new TestDrawableModel(1)));

            if (withPlaceholder)
                AddAssert("placeholder is displayed", () => backedDrawable.DisplayedDrawable is TestPlaceholder);
            else
                AddAssert("nothing displayed", () => backedDrawable.DisplayedDrawable == null);

            AddStep("allow first model to load", () => firstModel.AllowLoad.Set());
            AddUntilStep("first model displayed", () => backedDrawable.DisplayedDrawable == firstModel);

            AddStep("set second model", () => backedDrawable.Model = new TestModel(secondModel = new TestDrawableModel(2)));

            if (fadeOutImmediately)
            {
                if (withPlaceholder)
                    AddAssert("placeholder is displayed", () => backedDrawable.DisplayedDrawable is TestPlaceholder);
                else
                    AddAssert("nothing displayed", () => backedDrawable.DisplayedDrawable == null);
            }
            else
                AddAssert("first model still displayed", () => backedDrawable.DisplayedDrawable == firstModel);

            AddStep("allow second model to load", () => secondModel.AllowLoad.Set());
            AddUntilStep("second model displayed", () => backedDrawable.DisplayedDrawable == secondModel);
        }

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void TestSequentialDelayedLoad(bool withPlaceholder, bool fadeOutImmediately)
        {
            const int model_count = 3;

            var drawableModels = new List<TestDrawableModel>(model_count);

            AddStep("setup", () =>
            {
                drawableModels.Clear();
                createModelBackedDrawable(withPlaceholder, fadeOutImmediately);
            });

            for (int i = 0; i < model_count; i++)
            {
                int localI = i;
                AddStep($"set model {i + 1}", () =>
                {
                    var model = new TestDrawableModel(localI + 1);
                    drawableModels.Add(model);

                    backedDrawable.Model = new TestModel(model);
                });
            }

            // Due to potential left-over threading from elsewhere, we may have to wait for all models to get into a loading state
            AddUntilStep("all loading", () => drawableModels.TrueForAll(d => d.LoadState == LoadState.Loading));

            for (int i = 0; i < model_count - 1; i++)
            {
                int localI = i;
                AddStep($"allow model {i + 1} to load", () => drawableModels[localI].AllowLoad.Set());
                AddWaitStep("wait for potential load", 5);

                if (withPlaceholder)
                    AddAssert("placeholder displayed", () => backedDrawable.DisplayedDrawable is TestPlaceholder);
                else
                    AddAssert("no model displayed", () => backedDrawable.DisplayedDrawable == null);

                AddAssert($"model {i + 1} not loaded", () => !drawableModels[localI].IsLoaded);
            }

            AddStep($"allow model {model_count} to load", () => drawableModels[model_count - 1].AllowLoad.Set());
            AddUntilStep($"model {model_count} displayed", () => backedDrawable.DisplayedDrawable == drawableModels[model_count - 1]);
        }

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void TestOutOfOrderDelayedLoad(bool withPlaceholder, bool fadeOutImmediately)
        {
            const int model_count = 3;

            var drawableModels = new List<TestDrawableModel>(model_count);

            AddStep("setup", () =>
            {
                drawableModels.Clear();
                createModelBackedDrawable(withPlaceholder, fadeOutImmediately);
            });

            for (int i = 0; i < model_count; i++)
            {
                int localI = i;
                AddStep($"set model {i + 1}", () =>
                {
                    var model = new TestDrawableModel(localI + 1);
                    drawableModels.Add(model);

                    backedDrawable.Model = new TestModel(model);
                });
            }

            // Due to potential left-over threading from elsewhere, we may have to wait for all models to get into a loading state
            AddUntilStep("all loading", () => drawableModels.TrueForAll(d => d.LoadState == LoadState.Loading));

            AddStep($"allow model {model_count} to load", () => drawableModels[model_count - 1].AllowLoad.Set());
            AddUntilStep($"model {model_count} displayed", () => backedDrawable.DisplayedDrawable == drawableModels[model_count - 1]);

            for (int i = model_count - 2; i >= 0; i--)
            {
                int localI = i;
                AddStep($"allow model {i + 1} to load", () => drawableModels[localI].AllowLoad.Set());
                AddWaitStep("wait for potential load", 5);
                AddAssert($"model {model_count} still displayed", () => backedDrawable.DisplayedDrawable == drawableModels[model_count - 1]);
            }
        }

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void TestSetNullModel(bool withPlaceholder, bool fadeOutImmediately)
        {
            TestDrawableModel drawableModel = null;

            AddStep("setup", () => createModelBackedDrawable(withPlaceholder, fadeOutImmediately));

            AddStep("set model", () =>
            {
                backedDrawable.Model = new TestModel(drawableModel = new TestDrawableModel(1));
                drawableModel.AllowLoad.Set();
            });

            AddUntilStep("model is displayed", () => backedDrawable.DisplayedDrawable == drawableModel);

            AddStep("set null model", () => backedDrawable.Model = null);

            if (withPlaceholder)
                AddAssert("placeholder displayed", () => backedDrawable.DisplayedDrawable is TestPlaceholder);
            else
                AddAssert("no drawable displayed", () => backedDrawable.DisplayedDrawable == null);
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
                {
                }
            }
        }

        private class TestPlaceholder : TestDrawableModel
        {
            public TestPlaceholder()
                : base("Placeholder")
            {
                AllowLoad.Set();
            }
        }

        private class TestModelBackedDrawable : ModelBackedDrawable<TestModel>
        {
            protected override Drawable CreateDrawable(TestModel model)
            {
                if (model == null)
                    return HasPlaceholder ? new TestPlaceholder() : null;

                return model.DrawableModel;
            }

            public new Drawable DisplayedDrawable => base.DisplayedDrawable;

            public new TestModel Model
            {
                get => base.Model;
                set => base.Model = value;
            }

            public bool InternalFadeOutImmediately;

            public bool HasPlaceholder;

            protected override bool FadeOutImmediately => InternalFadeOutImmediately;
        }
    }
}
