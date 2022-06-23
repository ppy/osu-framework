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
    public class TestSceneModelBackedDrawableWithLoading : FrameworkTestScene
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

                CornerRadius = 5;
                Masking = true;

                AddRangeInternal(new[]
                {
                    new Box
                    {
                        Colour = new Color4(0.1f, 0.1f, 0.1f, 1),
                        RelativeSizeAxes = Axes.Both,
                        Depth = float.MaxValue
                    },
                    spinner = new LoadingSpinner
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Size = new Vector2(20),
                        Alpha = 0,
                        Depth = float.MinValue
                    }
                });
            }

            protected override bool TransformImmediately => immediate;

            protected override double TransformDuration => 500;

            protected override Drawable CreateDrawable(TestModel model) => model?.Drawable;

            protected override void OnLoadStarted()
            {
                base.OnLoadStarted();

                if (!immediate)
                    DisplayedDrawable?.FadeTo(0, 300, Easing.OutQuint);

                spinner.FadeIn(300, Easing.OutQuint);
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
            private static int id = 1;

            public readonly ManualResetEventSlim AllowLoad = new ManualResetEventSlim();

            protected Color4 BackgroundColour
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

            public TestDrawableModel()
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
                        Text = $"Model {id++}"
                    }
                };
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                if (!AllowLoad.Wait(10000))
                    throw new TimeoutException("Load was not allowed in a timely fashion");
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
