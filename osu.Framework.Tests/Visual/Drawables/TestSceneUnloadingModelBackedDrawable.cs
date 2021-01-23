// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

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
    [System.ComponentModel.Description("MBD behaviour with unloading wrapper")]
    public class TestSceneUnloadingModelBackedDrawable : FrameworkTestScene
    {
        private TestUnloadingModelBackedDrawable backedDrawable;
        private Drawable initialDrawable;

        [SetUpSteps]
        public void SetUpSteps()
        {
            AddStep("setup mbd", () =>
            {
                Child = backedDrawable = new TestUnloadingModelBackedDrawable(TimePerAction)
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Size = new Vector2(200f),
                    RelativePositionAxes = Axes.Both,
                };
            });

            AddWaitStep("wait for load", 1);
            AddStep("get displayed drawable", () => initialDrawable = backedDrawable.DisplayedDrawable);
        }

        [Test]
        public void TestUnloading()
        {
            AddStep("mask away", () => backedDrawable.Position = new Vector2(-1));
            AddWaitStep("wait for unload", 1);

            AddAssert("drawable unloaded", () => initialDrawable.IsDisposed && backedDrawable.DisplayedDrawable == null);

            AddStep("return back", () => backedDrawable.Position = Vector2.Zero);
            AddWaitStep("wait for reload", 1);

            AddAssert("new drawable displayed", () => backedDrawable.DisplayedDrawable != null && backedDrawable.DisplayedDrawable != initialDrawable);
        }

        [Test]
        public void TestChangeWhileMaskedAway()
        {
            AddStep("mask away", () => backedDrawable.Position = new Vector2(-1));
            AddWaitStep("wait for unload", 1);

            AddStep("change model", () => backedDrawable.Model = 5);
            AddWaitStep("wait for potential load", 5);

            AddAssert("new drawable not displayed", () => backedDrawable.DisplayedDrawable == null);

            AddStep("return back", () => backedDrawable.Position = Vector2.Zero);
            AddWaitStep("wait for load", 1);

            AddAssert("new drawable displayed", () => backedDrawable.DisplayedDrawable != null && backedDrawable.DisplayedDrawable != initialDrawable);
        }

        [Test]
        public void TestTransformsAppliedOnReloading()
        {
            AddStep("mask away", () => backedDrawable.Position = new Vector2(-1));
            AddWaitStep("wait for unload", 1);

            AddStep("reset transform counters", () => backedDrawable.ResetTransformCounters());
            AddStep("return back", () => backedDrawable.Position = Vector2.Zero);
            AddWaitStep("wait for reload", 1);

            // on loading, mbd applies immediate hide transform on new drawable then applies show transform.
            AddAssert("initial hide transform applied", () => backedDrawable.HideTransforms == 1);
            AddAssert("show transform applied", () => backedDrawable.ShowTransforms == 1);
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
