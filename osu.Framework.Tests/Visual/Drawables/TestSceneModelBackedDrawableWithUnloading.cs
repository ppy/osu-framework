// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Transforms;
using osu.Framework.Testing;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Drawables
{
    public class TestSceneModelBackedDrawableWithUnloading : FrameworkTestScene
    {
        private TestUnloadingModelBackedDrawable backedDrawable;
        private Drawable initialDrawable;

        [SetUpSteps]
        public void SetUpSteps()
        {
            AddStep("setup drawable", () =>
            {
                Child = backedDrawable = new TestUnloadingModelBackedDrawable(TimePerAction)
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Size = new Vector2(200f),
                    RelativePositionAxes = Axes.Both,
                };
            });

            AddUntilStep("wait for load", () => backedDrawable.DisplayedDrawable != null);
            AddStep("get displayed drawable", () => initialDrawable = backedDrawable.DisplayedDrawable);
        }

        [Test]
        public void TestUnloading()
        {
            AddStep("mask away", () => backedDrawable.Position = new Vector2(-2));
            AddUntilStep("drawable unloaded", () => initialDrawable.IsDisposed && backedDrawable.DisplayedDrawable == null);

            AddStep("return back", () => backedDrawable.Position = Vector2.Zero);
            AddUntilStep("new drawable displayed", () => backedDrawable.DisplayedDrawable != null && backedDrawable.DisplayedDrawable != initialDrawable);
        }

        [Test]
        public void TestChangeWhileMaskedAway()
        {
            AddStep("mask away", () => backedDrawable.Position = new Vector2(-2));
            AddUntilStep("wait for drawable unload", () => backedDrawable.DisplayedDrawable == null);

            AddStep("change model", () => backedDrawable.Model = 1);
            AddWaitStep("wait a bit", 5);
            AddAssert("no drawable loaded", () => backedDrawable.DisplayedDrawable == null);

            AddStep("return back", () => backedDrawable.Position = Vector2.Zero);
            AddUntilStep("new drawable displayed", () => backedDrawable.DisplayedDrawable != null);
        }

        [Test]
        public void TestTransformsAppliedOnReloading()
        {
            AddStep("mask away", () => backedDrawable.Position = new Vector2(-2));
            AddUntilStep("wait for drawable unload", () => backedDrawable.DisplayedDrawable == null);

            AddStep("reset transform counters", () => backedDrawable.ResetTransformCounters());
            AddStep("return back", () => backedDrawable.Position = Vector2.Zero);
            AddUntilStep("wait for new drawable", () => backedDrawable.DisplayedDrawable?.IsLoaded == true);

            // on loading, ModelBackedDrawable applies immediate hide transform on new drawable then applies show transform.
            AddAssert("initial hide transform applied", () => backedDrawable.HideTransforms == 1);
            AddAssert("show transform applied", () => backedDrawable.ShowTransforms == 1);
            AddUntilStep("new drawable alpha = 1", () => backedDrawable.DisplayedDrawable.Alpha == 1);
        }

        private class TestUnloadingModelBackedDrawable : ModelBackedDrawable<int>
        {
            public new Drawable DisplayedDrawable => base.DisplayedDrawable;

            public new int Model
            {
                get => base.Model;
                set => base.Model = value;
            }

            public int ShowTransforms { get; private set; }
            public int HideTransforms { get; private set; }

            protected override double LoadDelay { get; }
            protected double UnloadDelay { get; }

            public TestUnloadingModelBackedDrawable(double timePerAction)
            {
                LoadDelay = timePerAction;
                UnloadDelay = timePerAction;
            }

            public void ResetTransformCounters() => ShowTransforms = HideTransforms = 0;

            protected override DelayedLoadWrapper CreateDelayedLoadWrapper(Func<Drawable> createContentFunc, double timeBeforeLoad)
            {
                return new DelayedLoadUnloadWrapper(createContentFunc, timeBeforeLoad, UnloadDelay);
            }

            protected override TransformSequence<Drawable> ApplyShowTransforms(Drawable drawable)
            {
                ShowTransforms++;
                return base.ApplyShowTransforms(drawable);
            }

            protected override TransformSequence<Drawable> ApplyHideTransforms(Drawable drawable)
            {
                HideTransforms++;
                return base.ApplyHideTransforms(drawable);
            }

            protected override Drawable CreateDrawable(int model)
            {
                return new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Children = new Drawable[]
                    {
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = Color4.Gray,
                        },
                        new SpriteText
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Text = $"unload-wrapped model {model}",
                        }
                    }
                };
            }
        }
    }
}
