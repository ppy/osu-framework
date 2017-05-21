// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Testing;
using OpenTK;
using OpenTK.Graphics;

namespace osu.Framework.VisualTests.Tests
{
    public class TestCaseHollowEdgeEffect : TestCase
    {
        public override string Description => @"Hollow Container with EdgeEffect";

        public override void Reset()
        {
            base.Reset();

            Children = new Drawable[]
            {
                new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Children = new Drawable[]
                    {
                        new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Size = new Vector2(0.5f),
                            Children = new Drawable[]
                            {
                                new SpriteText { Text = @"Corner 0f | 0.5f Transparency" },
                                new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Size = new Vector2(0.5f),
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.Centre,
                                    Children = new Drawable[]
                                    {
                                        new Container()
                                        {
                                            Size = new Vector2(200f, 100f),
                                            Anchor = Anchor.Centre,
                                            Origin = Anchor.Centre,

                                            Masking = true,
                                            EdgeEffect = new EdgeEffect()
                                            {
                                                Type = EdgeEffectType.Glow,
                                                Colour = Color4.Khaki,
                                                Radius = 100f,
                                                Hollow = true
                                            },
                                            CornerRadius = 0f,

                                            Children = new Drawable[]
                                            {
                                                new Box()
                                                {
                                                    RelativeSizeAxes = Axes.Both,
                                                    Colour = Color4.Aqua,
                                                    Alpha = 0.5f
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        },
                        new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Size = new Vector2(0.5f),
                            Children = new Drawable[]
                            {
                                new SpriteText { Text = @"Corner 50f | 0.5f Transparency" },
                                new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Size = new Vector2(0.5f),
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.Centre,
                                    Children = new Drawable[]
                                    {
                                        new Container()
                                        {
                                            Size = new Vector2(100f),
                                            Anchor = Anchor.Centre,
                                            Origin = Anchor.Centre,

                                            Masking = true,
                                            EdgeEffect = new EdgeEffect()
                                            {
                                                Type = EdgeEffectType.Glow,
                                                Colour = Color4.Khaki,
                                                Radius = 100f,
                                                Hollow = true
                                            },
                                            CornerRadius = 50f,

                                            Children = new Drawable[]
                                            {
                                                new Box()
                                                {
                                                    RelativeSizeAxes = Axes.Both,
                                                    Colour = Color4.Aqua,
                                                    Alpha = 0.5f
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        },
                        new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Size = new Vector2(0.5f),
                            Children = new Drawable[]
                            {
                                new SpriteText { Text = @"Corner 0f | No fill" },
                                new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Size = new Vector2(0.5f),
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.Centre,
                                    Children = new Drawable[]
                                    {
                                        new Container()
                                        {
                                            Size = new Vector2(200f, 100f),
                                            Anchor = Anchor.Centre,
                                            Origin = Anchor.Centre,

                                            Masking = true,
                                            EdgeEffect = new EdgeEffect()
                                            {
                                                Type = EdgeEffectType.Glow,
                                                Colour = Color4.Khaki,
                                                Radius = 100f,
                                                Hollow = true
                                            },
                                            CornerRadius = 0f
                                        }
                                    }
                                }
                            }
                        },
                        new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Size = new Vector2(0.5f),
                            Children = new Drawable[]
                            {
                                new SpriteText { Text = @"Corner 50f | No fill" },
                                new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Size = new Vector2(0.5f),
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.Centre,
                                    Children = new Drawable[]
                                    {
                                        new Container()
                                        {
                                            Size = new Vector2(100f),
                                            Anchor = Anchor.Centre,
                                            Origin = Anchor.Centre,

                                            Masking = true,
                                            EdgeEffect = new EdgeEffect()
                                            {
                                                Type = EdgeEffectType.Glow,
                                                Colour = Color4.Khaki,
                                                Radius = 100f,
                                                Hollow = true
                                            },
                                            CornerRadius = 50f
                                        }
                                    }
                                }
                            }
                        },
                    }
                }
            };
        }
    }
}
