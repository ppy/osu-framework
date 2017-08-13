﻿// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Effects;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Testing;
using OpenTK;
using OpenTK.Graphics;

namespace osu.Framework.Desktop.Tests.Visual
{
    [TestFixture]
    internal class TestCaseEffects : TestCase
    {
        public override string Description => "Tests classes implement the IEffect interface.";

        public TestCaseEffects()
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
                        TextSize = 32f
                    }.WithEffect(new BlurEffect
                    {
                        Sigma = new Vector2(2f, 0f),
                        Strength = 2f,
                        Rotation = 45f,
                    }),
                    new SpriteText
                    {
                        Text = "EdgeEffect Test",
                        TextSize = 32f
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
                        TextSize = 32f
                    }.WithEffect(effect),
                    new SpriteText
                    {
                        Text = "Repeated usage of same effect test",
                        TextSize = 32f
                    }.WithEffect(effect),
                    new SpriteText
                    {
                        Text = "Repeated usage of same effect test",
                        TextSize = 32f
                    }.WithEffect(effect),
                    new SpriteText
                    {
                        Text = "Multiple effects Test",
                        TextSize = 32f
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
                                TextSize = 32f
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
                                TextSize = 32f,
                            }.WithEffect(new GlowEffect
                            {
                                BlurSigma = new Vector2(3f),
                                Strength = 3f,
                                Colour = ColourInfo.GradientHorizontal(new Color4(1.2f, 0, 0, 1f), new Color4(0, 1f, 0, 1f)),
                                PadExtent = true,
                            }),
                        }
                    },
                }
            });
        }
    }
}
