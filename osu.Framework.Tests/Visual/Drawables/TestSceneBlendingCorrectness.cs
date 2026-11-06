// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Drawables
{
    public partial class TestSceneBlendingCorrectness : FrameworkTestScene
    {
        [Resolved]
        private TextureStore textures { get; set; } = null!;

        [Test]
        public void TestMixture()
        {
            AddStep("setup", () =>
            {
                Child = new FillFlowContainer
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    AutoSizeAxes = Axes.Both,
                    Direction = FillDirection.Vertical,
                    Spacing = new Vector2(0, 20f),
                    Children = new Drawable[]
                    {
                        new Container
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            AutoSizeAxes = Axes.Both,
                            Children = new Drawable[]
                            {
                                new Container
                                {
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.CentreRight,
                                    Size = new Vector2(100, 200),
                                    Children = new Drawable[]
                                    {
                                        new Box
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                            Colour = ColourInfo.GradientVertical(Color4.Yellow, Color4.Blue)
                                        },
                                        new Container
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                            Children = new Drawable[]
                                            {
                                                new Box
                                                {
                                                    Anchor = Anchor.CentreRight,
                                                    Origin = Anchor.CentreRight,
                                                    RelativeSizeAxes = Axes.Both,
                                                    Size = new Vector2(0.5f, 1),
                                                    Colour = Color4.Cyan,
                                                    Alpha = 0.5f,
                                                },
                                                new Box
                                                {
                                                    Anchor = Anchor.CentreRight,
                                                    Origin = Anchor.CentreRight,
                                                    RelativeSizeAxes = Axes.Both,
                                                    Size = new Vector2(0.25f, 1),
                                                    Colour = Color4.Magenta,
                                                    Alpha = 0.5f
                                                }
                                            }
                                        }
                                    }
                                },
                                new Container
                                {
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.CentreLeft,
                                    Size = new Vector2(100, 200),
                                    Children = new Drawable[]
                                    {
                                        new Box
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                            Colour = ColourInfo.GradientVertical(Color4.Yellow, Color4.Blue)
                                        },
                                        new BufferedContainer
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                            Children = new Drawable[]
                                            {
                                                new Box
                                                {
                                                    Anchor = Anchor.CentreLeft,
                                                    Origin = Anchor.CentreLeft,
                                                    RelativeSizeAxes = Axes.Both,
                                                    Size = new Vector2(0.5f, 1),
                                                    Colour = Color4.Cyan,
                                                    Alpha = 0.5f,
                                                },
                                                new Box
                                                {
                                                    Anchor = Anchor.CentreLeft,
                                                    Origin = Anchor.CentreLeft,
                                                    RelativeSizeAxes = Axes.Both,
                                                    Size = new Vector2(0.25f, 1),
                                                    Colour = Color4.Magenta,
                                                    Alpha = 0.5f,
                                                }
                                            }
                                        }
                                    }
                                },
                                new SpriteText
                                {
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.Centre,
                                    Text = "Container",
                                    X = -150
                                },
                                new SpriteText
                                {
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.Centre,
                                    Text = "Buffered",
                                    X = 150
                                },
                            }
                        },
                        new Sprite
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Texture = textures.Get("figma-blending-mixture")
                        },
                        new SpriteText
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Text = "Figma design",
                        },
                    }
                };
            });
        }

        [Test]
        public void TestAdditive()
        {
            AddStep("setup", () =>
            {
                Child = new FillFlowContainer
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    AutoSizeAxes = Axes.Both,
                    Direction = FillDirection.Vertical,
                    Spacing = new Vector2(0, 20f),
                    Children = new Drawable[]
                    {
                        new Container
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            AutoSizeAxes = Axes.Both,
                            Children = new Drawable[]
                            {
                                new Container
                                {
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.CentreRight,
                                    Size = new Vector2(100, 200),
                                    Children = new Drawable[]
                                    {
                                        new Box
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                            Colour = ColourInfo.GradientVertical(Color4.Yellow, Color4.Blue)
                                        },
                                        new Container
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                            Blending = BlendingParameters.Additive,
                                            Children = new Drawable[]
                                            {
                                                new Box
                                                {
                                                    Anchor = Anchor.CentreRight,
                                                    Origin = Anchor.CentreRight,
                                                    RelativeSizeAxes = Axes.Both,
                                                    Size = new Vector2(0.5f, 1),
                                                    Colour = Color4.Cyan,
                                                    Alpha = 0.5f,
                                                },
                                                new Box
                                                {
                                                    Anchor = Anchor.CentreRight,
                                                    Origin = Anchor.CentreRight,
                                                    RelativeSizeAxes = Axes.Both,
                                                    Size = new Vector2(0.25f, 1),
                                                    Colour = Color4.Magenta,
                                                    Alpha = 0.5f
                                                }
                                            }
                                        }
                                    }
                                },
                                new Container
                                {
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.CentreLeft,
                                    Size = new Vector2(100, 200),
                                    Children = new Drawable[]
                                    {
                                        new Box
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                            Colour = ColourInfo.GradientVertical(Color4.Yellow, Color4.Blue)
                                        },
                                        new BufferedContainer
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                            Blending = BlendingParameters.Additive,
                                            Children = new Drawable[]
                                            {
                                                new Box
                                                {
                                                    Anchor = Anchor.CentreLeft,
                                                    Origin = Anchor.CentreLeft,
                                                    RelativeSizeAxes = Axes.Both,
                                                    Size = new Vector2(0.5f, 1),
                                                    Colour = Color4.Cyan,
                                                    Alpha = 0.5f,
                                                },
                                                new Box
                                                {
                                                    Anchor = Anchor.CentreLeft,
                                                    Origin = Anchor.CentreLeft,
                                                    RelativeSizeAxes = Axes.Both,
                                                    Size = new Vector2(0.25f, 1),
                                                    Colour = Color4.Magenta,
                                                    Alpha = 0.5f,
                                                }
                                            }
                                        }
                                    }
                                },
                                new SpriteText
                                {
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.Centre,
                                    Text = "Container",
                                    X = -150
                                },
                                new SpriteText
                                {
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.Centre,
                                    Text = "Buffered",
                                    X = 150
                                },
                            }
                        },
                        new Sprite
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Texture = textures.Get("figma-blending-additive")
                        },
                        new SpriteText
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Text = "Figma design",
                        },
                    }
                };
            });
        }

        /// <summary>
        /// There is no reference source for this one. This can be used to test whether <see cref="BlendingType.One"/> or <see cref="BlendingType.OneMinusSrcAlpha"/> is more correct
        /// to use as the blending type for <see cref="BlendingParameters.DestinationAlpha"/>.
        /// </summary>
        [Test]
        public void TestAdditiveAlpha()
        {
            AddStep("setup", () =>
            {
                Child = new Container
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Size = new Vector2(200, 200),
                    Children = new Drawable[]
                    {
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = Color4.Blue,
                            Alpha = 0.5f,
                        },
                        new BufferedContainer
                        {
                            RelativeSizeAxes = Axes.Both,
                            Children = new Drawable[]
                            {
                                new Box
                                {
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.Centre,
                                    RelativeSizeAxes = Axes.Both,
                                    Size = new Vector2(0.5f, 1),
                                    Blending = BlendingParameters.Additive,
                                    Colour = Color4.White,
                                    Alpha = 0.5f,
                                },
                                new Box
                                {
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.Centre,
                                    RelativeSizeAxes = Axes.Both,
                                    Size = new Vector2(0.25f, 1),
                                    Blending = BlendingParameters.Additive,
                                    Colour = Color4.White,
                                    Alpha = 0.25f,
                                }
                            }
                        }
                    }
                };
            });
        }
    }
}
