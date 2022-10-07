// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Effects;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Testing;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Drawables
{
    public class TestSceneHollowEdgeEffect : GridTestScene
    {
        public TestSceneHollowEdgeEffect()
            : base(2, 2)
        {
            const float size = 60;

            float[] cornerRadii = { 0, 0.5f, 0, 0.5f };
            float[] alphas = { 0.5f, 0.5f, 0, 0 };
            EdgeEffectParameters[] edgeEffects =
            {
                new EdgeEffectParameters
                {
                    Type = EdgeEffectType.Glow,
                    Colour = Color4.Khaki,
                    Radius = size,
                    Hollow = true,
                },
                new EdgeEffectParameters
                {
                    Type = EdgeEffectType.Glow,
                    Colour = Color4.Khaki,
                    Radius = size,
                    Hollow = true,
                },
                new EdgeEffectParameters
                {
                    Type = EdgeEffectType.Glow,
                    Colour = Color4.Khaki,
                    Radius = size,
                    Hollow = true,
                },
                new EdgeEffectParameters
                {
                    Type = EdgeEffectType.Glow,
                    Colour = Color4.Khaki,
                    Radius = size,
                    Hollow = true,
                },
            };

            for (int i = 0; i < Rows * Cols; ++i)
            {
                Cell(i).AddRange(new Drawable[]
                {
                    new SpriteText
                    {
                        Text = $"{nameof(CornerRadius)}={cornerRadii[i]} {nameof(Alpha)}={alphas[i]}",
                        Font = new FontUsage(size: 20),
                    },
                    new Container
                    {
                        Size = new Vector2(size),
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,

                        Masking = true,
                        EdgeEffect = edgeEffects[i],
                        CornerRadius = cornerRadii[i] * size,

                        Children = new Drawable[]
                        {
                            new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Colour = Color4.Aqua,
                                Alpha = alphas[i],
                            },
                        },
                    },
                });
            }
        }
    }
}
