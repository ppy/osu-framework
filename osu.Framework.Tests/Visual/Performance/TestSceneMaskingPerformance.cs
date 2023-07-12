// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Effects;
using osu.Framework.Graphics.Shapes;
using Container = osu.Framework.Graphics.Containers.Container;

namespace osu.Framework.Tests.Visual.Performance
{
    public partial class TestSceneMaskingPerformance : RepeatedDrawablePerformanceTestScene
    {
        private readonly BindableFloat cornerRadius = new BindableFloat();
        private readonly BindableFloat cornerExponent = new BindableFloat(2f);
        private readonly Bindable<EdgeEffectParameters> edgeEffectParameters = new Bindable<EdgeEffectParameters>();

        protected override void LoadComplete()
        {
            base.LoadComplete();

            AddLabel("Masking & Edge Effects");
            AddSliderStep("corner radius", 0f, 100f, 0f, v => cornerRadius.Value = v);
            AddSliderStep("corner exponent", 1f, 10f, 2f, v => cornerExponent.Value = v);
            AddStep("disable edge effect", () => edgeEffectParameters.Value = edgeEffectParameters.Value with { Type = EdgeEffectType.None });
            AddStep("glow edge effect", () => edgeEffectParameters.Value = edgeEffectParameters.Value with { Type = EdgeEffectType.Glow });
            AddStep("shadow edge effect", () => edgeEffectParameters.Value = edgeEffectParameters.Value with { Type = EdgeEffectType.Shadow });
            AddSliderStep("edge effect roundedness", 0f, 100f, 0f, v => edgeEffectParameters.Value = edgeEffectParameters.Value with { Roundness = v });
            AddSliderStep("edge effect radius", 0f, 100f, 0f, v => edgeEffectParameters.Value = edgeEffectParameters.Value with { Radius = v });
            AddToggleStep("edge effect hollow", v => edgeEffectParameters.Value = edgeEffectParameters.Value with { Hollow = v });
        }

        protected override Drawable CreateDrawable() => new TestContainer
        {
            CornerRadiusBindable = { BindTarget = cornerRadius },
            CornerExponentBindable = { BindTarget = cornerExponent },
            EdgeEffectParameters = { BindTarget = edgeEffectParameters },
        };

        private partial class TestContainer : Container
        {
            public readonly Bindable<float> CornerRadiusBindable = new BindableFloat();
            public readonly Bindable<float> CornerExponentBindable = new BindableFloat();
            public readonly Bindable<EdgeEffectParameters> EdgeEffectParameters = new Bindable<EdgeEffectParameters>();

            private readonly Drawable box;

            public TestContainer()
            {
                Child = box = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                };
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                Masking = true;

                CornerRadiusBindable.BindValueChanged(v => CornerRadius = v.NewValue, true);
                CornerExponentBindable.BindValueChanged(v => CornerExponent = v.NewValue, true);
                EdgeEffectParameters.BindValueChanged(v => EdgeEffect = v.NewValue, true);
            }

            protected override void Update()
            {
                base.Update();

                EdgeEffect = EdgeEffect with { Colour = box.Colour.AverageColour };
            }
        }
    }
}
