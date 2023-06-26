// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.ComponentModel;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Effects;
using osuTK;
using Container = osu.Framework.Graphics.Containers.Container;

namespace osu.Framework.Tests.Visual.Performance
{
    [Description("tests masking overhead")]
    public partial class TestSceneEdgeEffects : TestSceneBoxes
    {
        private float cornerRadius;
        private float cornerExponent;
        private EdgeEffectType edgeEffectType;
        private float edgeEffectRoundedness;
        private float edgeEffectRadius;
        private bool edgeEffectHollow;

        protected override void LoadComplete()
        {
            base.LoadComplete();

            AddSliderStep("corner radius", 0f, 100f, 0f, v => cornerRadius = v);
            AddSliderStep("corner exponent", 1f, 10f, 2f, v => cornerExponent = v);
            AddStep("disable edge effect", () => edgeEffectType = EdgeEffectType.None);
            AddStep("glow edge effect", () => edgeEffectType = EdgeEffectType.Glow);
            AddStep("shadow edge effect", () => edgeEffectType = EdgeEffectType.Shadow);
            AddSliderStep("edge effect roundedness", 0f, 100f, 0f, v => edgeEffectRoundedness = v);
            AddSliderStep("edge effect radius", 0f, 100f, 0f, v => edgeEffectRadius = v);
            AddToggleStep("edge effect hollow", v => edgeEffectHollow = v);
        }

        protected override Drawable CreateDrawable()
        {
            var sprite = base.CreateDrawable();

            var size = sprite.Size;
            sprite.Size = new Vector2(1f);

            return new Container
            {
                Masking = true,
                CornerRadius = cornerRadius,
                CornerExponent = cornerExponent,
                EdgeEffect = new EdgeEffectParameters
                {
                    Type = edgeEffectType,
                    Roundness = edgeEffectRoundedness,
                    Radius = edgeEffectRadius,
                    Hollow = edgeEffectHollow,
                    Colour = sprite.Colour.AverageColour,
                },
                RelativeSizeAxes = Axes.Both,
                Size = size,
                Child = sprite,
            };
        }
    }
}
