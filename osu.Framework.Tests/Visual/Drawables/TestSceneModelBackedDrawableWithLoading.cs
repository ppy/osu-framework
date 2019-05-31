// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

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
    public class TestSceneModelBackedDrawableWithLoading : TestScene
    {
        private TestModelBackedDrawable backedDrawable;

        private void createModelBackedDrawable(bool immediate) =>
            Child = backedDrawable = new TestModelBackedDrawable(immediate)
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Size = new Vector2(200),
            };

        [Test]
        public void TestNonImmediateTransform()
        {
            AddStep("setup", () => createModelBackedDrawable(false));
            addUsageSteps();
        }

        [Test]
        public void TestTransformImmediately()
        {
            AddStep("setup", () => createModelBackedDrawable(true));
            addUsageSteps();
        }

        private void addUsageSteps()
        {
            TestDrawableModel drawableModel = null;

            AddStep("load first model", () => backedDrawable.Model = new TestModel(drawableModel = new TestDrawableModel()));
            AddWaitStep("wait a bit", 5);
            AddStep("finish load", () => drawableModel.AllowLoad.Set());
            AddWaitStep("wait a bit", 5);
            AddStep("load second model", () => backedDrawable.Model = new TestModel(drawableModel = new TestDrawableModel()));
            AddWaitStep("wait a bit", 5);
            AddStep("finish load", () => drawableModel.AllowLoad.Set());
        }

        private class TestModelBackedDrawable : ModelBackedDrawable<TestModel>
        {
            public new TestModel Model
            {
                set => base.Model = value;
            }

            private readonly bool immediate;
            private readonly Drawable spinner;

            public TestModelBackedDrawable(bool immediate)
            {
                this.immediate = immediate;
                AddInternal(spinner = new LoadingSpinner
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Size = new Vector2(50),
                    Alpha = 0,
                    Depth = float.MinValue
                });
            }

            protected override bool TransformImmediately => immediate;

            protected override double TransformDuration => 500;

            protected override Drawable CreateDrawable(TestModel model) => model?.Drawable;

            protected override void OnLoadStarted()
            {
                base.OnLoadStarted();

                if (!immediate)
                    DisplayedDrawable?.FadeTo(0.5f, 500, Easing.OutQuint);

                spinner.Delay(250).FadeIn(500, Easing.OutQuint);
            }

            protected override void OnLoadFinished()
            {
                base.OnLoadFinished();

                spinner.FadeOut(250, Easing.OutQuint);
            }
        }

        private class TestModel
        {
            public readonly Drawable Drawable;

            public TestModel(TestDrawableModel drawable)
            {
                Drawable = drawable;
            }
        }

        private class TestDrawableModel : CompositeDrawable
        {
            private static int modelID = 1;

            public readonly ManualResetEventSlim AllowLoad = new ManualResetEventSlim();

            public TestDrawableModel()
            {
                RelativeSizeAxes = Axes.Both;

                InternalChildren = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.SlateGray
                    },
                    new SpriteText
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Text = $"Model {modelID++}"
                    }
                };
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                AllowLoad.Wait();
            }
        }

        private class LoadingSpinner : CompositeDrawable
        {
            private readonly SpriteIcon icon;

            public LoadingSpinner()
            {
                InternalChild = icon = new SpriteIcon
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    RelativeSizeAxes = Axes.Both,
                    Icon = FontAwesome.Solid.Spinner
                };
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();
                icon.Spin(2000, RotationDirection.Clockwise);
            }
        }
    }
}
