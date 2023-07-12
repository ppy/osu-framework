// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Shapes;
using osuTK;

namespace osu.Framework.Tests.Visual.Performance
{
    public partial class TestSceneBlendingPerformance : RepeatedDrawablePerformanceTestScene
    {
        private readonly BindableFloat alpha = new BindableFloat();
        private readonly Bindable<BlendingParameters> blendingParameters = new Bindable<BlendingParameters>();

        protected override void LoadComplete()
        {
            base.LoadComplete();

            AddLabel("Blending");
            AddSliderStep("spacing", -20, 20f, -1f, v => Flow.Spacing = new Vector2(v));
            AddSliderStep("alpha", 0f, 1f, 0.9f, v => alpha.Value = v);
            AddStep("disable blending", () => blendingParameters.Value = BlendingParameters.None);
            AddStep("set additive blending", () => blendingParameters.Value = BlendingParameters.Additive);
            AddStep("set mixture blending", () => blendingParameters.Value = BlendingParameters.Mixture);
        }

        protected override Drawable CreateDrawable() => new TestBlendingBox
        {
            BlendingParameters = { BindTarget = blendingParameters },
            AlphaBindable = { BindTarget = alpha },
        };

        private partial class TestBlendingBox : Box
        {
            public readonly IBindable<BlendingParameters> BlendingParameters = new Bindable<BlendingParameters>();
            public readonly IBindable<float> AlphaBindable = new Bindable<float>();

            [BackgroundDependencyLoader]
            private void load()
            {
                AlphaBindable.BindValueChanged(v => Alpha = v.NewValue, true);
                BlendingParameters.BindValueChanged(v => Blending = v.NewValue, true);
            }
        }
    }
}
