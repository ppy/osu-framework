// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Effects;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Drawables
{
    [System.ComponentModel.Description("implementing the IEffect interface")]
    public class TestSceneEffects : FrameworkTestScene
    {
        private readonly SpriteText changeColourText;

        public TestSceneEffects()
        {
            var effect = new EdgeEffect
            {
                CornerRadius = 3f,
                Parameters = new EdgeEffectParameters
                {
                    Colour = Color4.LightBlue,
                    Hollow = true,
                    Radius = 5f,
                    Type = EdgeEffectType.Glow
                }
            };
            Add(new FillFlowContainer
            {
                Position = new Vector2(10f, 10f),
                Spacing = new Vector2(25f, 25f),
                RelativeSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    new SpriteText
                    {
                        Text = "Blur Test",
                        Font = new FontUsage(size: 32),
                    }.WithEffect(new BlurEffect
                    {
                        Sigma = new Vector2(2f, 0f),
                        Strength = 2f,
                        Rotation = 45f,
                    }),
                    new SpriteText
                    {
                        Text = "EdgeEffect Test",
                        Font = new FontUsage(size: 32),
                    }.WithEffect(new EdgeEffect
                    {
                        CornerRadius = 3f,
                        Parameters = new EdgeEffectParameters
                        {
                            Colour = Color4.Yellow,
                            Hollow = true,
                            Radius = 5f,
                            Type = EdgeEffectType.Shadow
                        }
                    }),
                    new SpriteText
                    {
                        Text = "Repeated usage of same effect test",
                        Font = new FontUsage(size: 32),
                    }.WithEffect(effect),
                    new SpriteText
                    {
                        Text = "Repeated usage of same effect test",
                        Font = new FontUsage(size: 32),
                    }.WithEffect(effect),
                    new SpriteText
                    {
                        Text = "Repeated usage of same effect test",
                        Font = new FontUsage(size: 32),
                    }.WithEffect(effect),
                    new SpriteText
                    {
                        Text = "Multiple effects Test",
                        Font = new FontUsage(size: 32),
                    }.WithEffect(new BlurEffect
                    {
                        Sigma = new Vector2(2f, 2f),
                        Strength = 2f
                    }).WithEffect(new EdgeEffect
                    {
                        CornerRadius = 3f,
                        Parameters = new EdgeEffectParameters
                        {
                            Colour = Color4.Yellow,
                            Hollow = true,
                            Radius = 5f,
                            Type = EdgeEffectType.Shadow
                        }
                    }),
                    new Container
                    {
                        AutoSizeAxes = Axes.Both,
                        Children = new Drawable[]
                        {
                            new Box
                            {
                                Colour = Color4.CornflowerBlue,
                                RelativeSizeAxes = Axes.Both,
                            },
                            new SpriteText
                            {
                                Text = "Outlined Text",
                                Font = new FontUsage(size: 32),
                            }.WithEffect(new OutlineEffect
                            {
                                BlurSigma = new Vector2(3f),
                                Strength = 3f,
                                Colour = Color4.Red,
                                PadExtent = true,
                            })
                        }
                    },
                    new Container
                    {
                        AutoSizeAxes = Axes.Both,
                        Children = new Drawable[]
                        {
                            new Box
                            {
                                Colour = Color4.CornflowerBlue,
                                RelativeSizeAxes = Axes.Both,
                            },
                            new SpriteText
                            {
                                Text = "Glowing Text",
                                Font = new FontUsage(size: 32),
                            }.WithEffect(new GlowEffect
                            {
                                BlurSigma = new Vector2(3f),
                                Strength = 3f,
                                Colour = ColourInfo.GradientHorizontal(new Color4(1.2f, 0, 0, 1f), new Color4(0, 1f, 0, 1f)),
                                PadExtent = true,
                            }),
                        }
                    },
                    new Container
                    {
                        AutoSizeAxes = Axes.Both,
                        Children = new Drawable[]
                        {
                            new Box
                            {
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre,
                                Colour = Color4.White,
                                Size = new Vector2(150, 40),
                            }.WithEffect(new GlowEffect
                            {
                                BlurSigma = new Vector2(3f),
                                Strength = 3f,
                                Colour = ColourInfo.GradientHorizontal(new Color4(1.2f, 0, 0, 1f), new Color4(0, 1f, 0, 1f)),
                                PadExtent = true,
                            }),
                            changeColourText = new SpriteText
                            {
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre,
                                Text = "Absolute Size",
                                Font = new FontUsage(size: 32),
                                Colour = Color4.Red,
                                Shadow = true,
                            }
                        }
                    },
                    new Container
                    {
                        AutoSizeAxes = Axes.Both,
                        Children = new Drawable[]
                        {
                            new Box
                            {
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre,
                                Colour = Color4.White,
                                RelativeSizeAxes = Axes.Both,
                                Size = new Vector2(1.1f, 1.1f),
                            }.WithEffect(new GlowEffect
                            {
                                BlurSigma = new Vector2(3f),
                                Strength = 3f,
                                Colour = ColourInfo.GradientHorizontal(new Color4(1.2f, 0, 0, 1f), new Color4(0, 1f, 0, 1f)),
                                PadExtent = true,
                            }),
                            new SpriteText
                            {
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre,
                                Text = "Relative Size",
                                Font = new FontUsage(size: 32),
                                Colour = Color4.Red,
                                Shadow = true,
                            },
                        }
                    },
                    new Container
                    {
                        AutoSizeAxes = Axes.Both,
                        Children = new Drawable[]
                        {
                            new Box
                            {
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre,
                                Colour = Color4.White,
                                RelativeSizeAxes = Axes.Both,
                                Size = new Vector2(1.1f, 1.1f),
                                Rotation = 10,
                            }.WithEffect(new GlowEffect
                            {
                                BlurSigma = new Vector2(3f),
                                Strength = 3f,
                                Colour = ColourInfo.GradientHorizontal(new Color4(1.2f, 0, 0, 1f), new Color4(0, 1f, 0, 1f)),
                                PadExtent = true,
                            }),
                            new SpriteText
                            {
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre,
                                Text = "Rotation",
                                Font = new FontUsage(size: 32),
                                Colour = Color4.Red,
                                Shadow = true,
                            },
                        }
                    },
                }
            });
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            changeColourText.FadeColour(Color4.Black, 1000).Then().FadeColour(Color4.Red, 1000).Loop();
        }
    }
}
