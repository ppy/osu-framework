// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Utils;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Performance
{
    public partial class TestSceneFillPerformance : PerformanceTestScene
    {
        private readonly BindableFloat alpha = new BindableFloat();
        private readonly Bindable<BlendingParameters> blendingParameters = new Bindable<BlendingParameters>();

        protected Drawable CreateDrawable() => new TestBlendingBox
        {
            BlendingParameters = { BindTarget = blendingParameters },
            AlphaBindable = { BindTarget = alpha },
        };

        private partial class TestBlendingBox : Box
        {
            public readonly IBindable<BlendingParameters> BlendingParameters = new Bindable<BlendingParameters>();
            public readonly IBindable<float> AlphaBindable = new Bindable<float>();

            public TestBlendingBox()
            {
                RelativeSizeAxes = Axes.Both;
                Anchor = Anchor.Centre;
                Origin = Anchor.Centre;
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                AlphaBindable.BindValueChanged(v => Alpha = v.NewValue, true);
                BlendingParameters.BindValueChanged(v => Blending = v.NewValue, true);
            }
        }

        protected readonly BindableInt DrawableCount = new BindableInt();
        protected readonly BindableBool GradientColour = new BindableBool();
        protected readonly BindableBool RandomiseColour = new BindableBool();

        protected override void LoadComplete()
        {
            base.LoadComplete();

            AddLabel("Drawables");

            AddSliderStep("count", 1, 100, 1000, v => DrawableCount.Value = v);
            AddToggleStep("gradient colour", v => GradientColour.Value = v);
            AddToggleStep("randomise colour", v => RandomiseColour.Value = v);

            DrawableCount.BindValueChanged(_ => adjustDrawableCount(), true);
            GradientColour.BindValueChanged(_ => updateMetrics());
            RandomiseColour.BindValueChanged(_ => updateMetrics());

            AddLabel("Blending");
            AddSliderStep("alpha", 0f, 1f, 0.01f, v => alpha.Value = v);
            AddStep("disable blending", () => blendingParameters.Value = BlendingParameters.None);
            AddStep("set additive blending", () => blendingParameters.Value = BlendingParameters.Additive);
            AddStep("set mixture blending", () => blendingParameters.Value = BlendingParameters.Mixture);
        }

        protected void Recreate()
        {
            Content.Clear();
            adjustDrawableCount();
        }

        private void adjustDrawableCount()
        {
            while (Content.Count > DrawableCount.Value)
                Content.Remove(Content.Children.Last(), true);

            while (Content.Count < DrawableCount.Value)
            {
                var drawable = CreateDrawable();
                updateMetrics(drawable);
                Content.Add(drawable);
            }
        }

        private void updateMetrics()
        {
            foreach (var b in Content)
                updateMetrics(b);
        }

        private void updateMetrics(Drawable drawable)
        {
            if (GradientColour.Value)
            {
                drawable.Colour = new ColourInfo
                {
                    TopLeft = RandomiseColour.Value ? getRandomColour() : Color4.Red,
                    TopRight = RandomiseColour.Value ? getRandomColour() : Color4.Blue,
                    BottomLeft = RandomiseColour.Value ? getRandomColour() : Color4.Green,
                    BottomRight = RandomiseColour.Value ? getRandomColour() : Color4.Yellow
                };
            }
            else
                drawable.Colour = RandomiseColour.Value ? getRandomColour() : Color4.White;
        }

        private Colour4 getRandomColour() => new Colour4(RNG.NextSingle(), RNG.NextSingle(), RNG.NextSingle(), 1f);
    }
}
