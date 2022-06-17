// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Effects;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Containers
{
    public class TestSceneMasking : FrameworkTestScene
    {
        protected Container TestContainer;
        protected int CurrentTest;
        protected float TestCornerExponent = 2f;

        public TestSceneMasking()
        {
            Add(TestContainer = new Container
            {
                RelativeSizeAxes = Axes.Both,
            });

            string[] testNames =
            {
                @"Round corner masking",
                @"Round corner AABB 1",
                @"Round corner AABB 2",
                @"Round corner AABB 3",
                @"Edge/border blurriness",
                @"Nested masking",
                @"Rounded corner input",
                @"Offset shadow",
                @"Negative size"
            };

            for (int i = 0; i < testNames.Length; i++)
            {
                int test = i;
                AddStep(testNames[i], delegate { loadTest(test); });
            }

            AddSliderStep("Corner exponent", 0.01f, 10, 2, exponent =>
            {
                TestCornerExponent = exponent;
                loadTest(CurrentTest);
            });

            loadTest(0);
            addCrosshair();
        }

        private void addCrosshair()
        {
            Add(new Box
            {
                Colour = Color4.Black,
                Size = new Vector2(22, 4),
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre
            });

            Add(new Box
            {
                Colour = Color4.Black,
                Size = new Vector2(4, 22),
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre
            });

            Add(new Box
            {
                Colour = Color4.WhiteSmoke,
                Size = new Vector2(20, 2),
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre
            });

            Add(new Box
            {
                Colour = Color4.WhiteSmoke,
                Size = new Vector2(2, 20),
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre
            });
        }

        private void loadTest(int testType)
        {
            TestContainer.Clear();
            CurrentTest = testType;

            switch (testType)
            {
                default:
                {
                    Container box;
                    TestContainer.Add(box = new InfofulBoxAutoSize
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Masking = true,
                        CornerRadius = 100,
                        CornerExponent = TestCornerExponent,
                        BorderColour = Color4.Aquamarine,
                        BorderThickness = 3,
                        EdgeEffect = new EdgeEffectParameters
                        {
                            Type = EdgeEffectType.Shadow,
                            Radius = 100,
                            Colour = new Color4(0, 50, 100, 200),
                        },
                    });

                    box.Add(box = new InfofulBox
                    {
                        Size = new Vector2(250, 250),
                        Alpha = 0.5f,
                        Origin = Anchor.Centre,
                        Anchor = Anchor.Centre,
                        Colour = Color4.DarkSeaGreen,
                    });

                    box.OnUpdate += delegate { box.Rotation += 0.05f; };
                    break;
                }

                case 1:
                {
                    Container box;
                    TestContainer.Add(new InfofulBoxAutoSize
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Children = new[]
                        {
                            box = new InfofulBox
                            {
                                Masking = true,
                                CornerRadius = 100,
                                CornerExponent = TestCornerExponent,
                                Size = new Vector2(400, 400),
                                Alpha = 0.5f,
                                Origin = Anchor.Centre,
                                Anchor = Anchor.Centre,
                                Colour = Color4.DarkSeaGreen,
                            }
                        }
                    });

                    box.OnUpdate += delegate
                    {
                        box.Rotation += 0.05f;
                        box.CornerRadius = 100 + 100 * MathF.Sin(box.Rotation * 0.01f);
                    };
                    break;
                }

                case 2:
                {
                    Container box;
                    TestContainer.Add(new InfofulBoxAutoSize
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Children = new[]
                        {
                            box = new InfofulBox
                            {
                                Masking = true,
                                CornerRadius = 25,
                                CornerExponent = TestCornerExponent,
                                Shear = new Vector2(0.5f, 0),
                                Size = new Vector2(150, 150),
                                Scale = new Vector2(2.5f, 1.5f),
                                Alpha = 0.5f,
                                Origin = Anchor.Centre,
                                Anchor = Anchor.Centre,
                                Colour = Color4.DarkSeaGreen,
                            }
                        }
                    });

                    box.OnUpdate += delegate { box.Rotation += 0.05f; };
                    break;
                }

                case 3:
                {
                    Color4 glowColour = Color4.Aquamarine;
                    glowColour.A = 0.5f;

                    Container box1;
                    Container box2;

                    TestContainer.Add(new InfofulBoxAutoSize
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        EdgeEffect = new EdgeEffectParameters
                        {
                            Type = EdgeEffectType.Glow,
                            Radius = 100,
                            Roundness = 50,
                            Colour = glowColour,
                        },
                        BorderColour = Color4.Aquamarine,
                        BorderThickness = 3,
                        Children = new[]
                        {
                            box1 = new InfofulBoxAutoSize
                            {
                                Masking = true,
                                CornerRadius = 25,
                                CornerExponent = TestCornerExponent,
                                Shear = new Vector2(0.5f, 0),
                                Alpha = 0.5f,
                                Origin = Anchor.Centre,
                                Anchor = Anchor.Centre,
                                Colour = Color4.DarkSeaGreen,
                                Children = new[]
                                {
                                    box2 = new InfofulBox
                                    {
                                        Masking = true,
                                        CornerRadius = 25,
                                        CornerExponent = TestCornerExponent,
                                        Shear = new Vector2(0.25f, 0.25f),
                                        Size = new Vector2(100, 200),
                                        Alpha = 0.5f,
                                        Origin = Anchor.Centre,
                                        Anchor = Anchor.Centre,
                                        Colour = Color4.Blue,
                                    }
                                }
                            }
                        }
                    });

                    box1.OnUpdate += delegate { box1.Rotation += 0.07f; };
                    box2.OnUpdate += delegate { box2.Rotation -= 0.15f; };
                    break;
                }

                case 4:
                {
                    static Drawable createMaskingBox(float scale, float testCornerExponent)
                    {
                        float size = 200 / scale;
                        return new Container
                        {
                            Masking = true,
                            CornerRadius = 25 / scale,
                            CornerExponent = testCornerExponent,
                            BorderThickness = 12.5f / scale,
                            BorderColour = Color4.Red,
                            Size = new Vector2(size),
                            Scale = new Vector2(scale),
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Children = new Drawable[]
                            {
                                new Box
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Colour = Color4.White,
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.Centre,
                                },
                                new SpriteText
                                {
                                    Text = @"Size: " + size + ", Scale: " + scale,
                                    Font = new FontUsage(size: 20 / scale),
                                    Colour = Color4.Blue,
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.Centre,
                                },
                            }
                        };
                    }

                    TestContainer.Add(new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        Children = new[]
                        {
                            new Container
                            {
                                RelativeSizeAxes = Axes.Both,
                                Size = new Vector2(0.5f),
                                Masking = true,
                                Children = new[] { createMaskingBox(100, TestCornerExponent) }
                            },
                            new Container
                            {
                                RelativeSizeAxes = Axes.Both,
                                Size = new Vector2(0.5f),
                                Masking = true,
                                Children = new[] { createMaskingBox(10, TestCornerExponent) }
                            },
                            new Container
                            {
                                RelativeSizeAxes = Axes.Both,
                                Size = new Vector2(0.5f),
                                Masking = true,
                                Children = new[] { createMaskingBox(1, TestCornerExponent) }
                            },
                            new Container
                            {
                                RelativeSizeAxes = Axes.Both,
                                Size = new Vector2(0.5f),
                                Masking = true,
                                Children = new[] { createMaskingBox(0.1f, TestCornerExponent) }
                            },
                        }
                    });

                    break;
                }

                case 5:
                {
                    TestContainer.Add(new Container
                    {
                        Masking = true,
                        Size = new Vector2(0.5f),
                        RelativeSizeAxes = Axes.Both,
                        Children = new[]
                        {
                            new Container
                            {
                                Masking = true,
                                CornerRadius = 100f,
                                CornerExponent = TestCornerExponent,
                                BorderThickness = 50f,
                                BorderColour = Color4.Red,
                                RelativeSizeAxes = Axes.Both,
                                Size = new Vector2(1.5f),
                                Anchor = Anchor.BottomRight,
                                Origin = Anchor.Centre,
                                Children = new Drawable[]
                                {
                                    new Box
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Colour = Color4.White,
                                    },
                                }
                            }
                        }
                    });
                    break;
                }

                case 6:
                {
                    TestContainer.Add(new FillFlowContainer
                    {
                        Direction = FillDirection.Vertical,
                        AutoSizeAxes = Axes.Both,
                        Spacing = new Vector2(0, 10),
                        Children = new Drawable[]
                        {
                            new SpriteText
                            {
                                Text = $"None of the folowing {nameof(CircularContainer)}s should trigger until the white part is hovered"
                            },
                            new FillFlowContainer
                            {
                                Direction = FillDirection.Vertical,
                                AutoSizeAxes = Axes.Both,
                                Spacing = new Vector2(0, 2),
                                Children = new Drawable[]
                                {
                                    new SpriteText
                                    {
                                        Text = "No masking"
                                    },
                                    new CircularContainerWithInput
                                    {
                                        Size = new Vector2(200),
                                        Children = new Drawable[]
                                        {
                                            new Box
                                            {
                                                RelativeSizeAxes = Axes.Both,
                                                Colour = Color4.Red
                                            },
                                            new CircularContainer
                                            {
                                                RelativeSizeAxes = Axes.Both,
                                                Colour = Color4.White,
                                                Masking = true,
                                                Children = new[]
                                                {
                                                    new Box
                                                    {
                                                        RelativeSizeAxes = Axes.Both
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            },
                            new FillFlowContainer
                            {
                                Direction = FillDirection.Vertical,
                                AutoSizeAxes = Axes.Both,
                                Spacing = new Vector2(0, 2),
                                Children = new Drawable[]
                                {
                                    new SpriteText
                                    {
                                        Text = "With masking"
                                    },
                                    new CircularContainerWithInput
                                    {
                                        Size = new Vector2(200),
                                        Masking = true,
                                        Children = new Drawable[]
                                        {
                                            new Box
                                            {
                                                RelativeSizeAxes = Axes.Both,
                                                Colour = Color4.Red
                                            },
                                            new CircularContainer
                                            {
                                                RelativeSizeAxes = Axes.Both,
                                                Colour = Color4.White,
                                                Masking = true,
                                                Children = new[]
                                                {
                                                    new Box
                                                    {
                                                        RelativeSizeAxes = Axes.Both
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    });
                    break;
                }

                case 7:
                {
                    Container box;
                    TestContainer.Add(box = new InfofulBoxAutoSize
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Masking = true,
                        CornerRadius = 100,
                        CornerExponent = TestCornerExponent,
                        Alpha = 0.8f,
                        EdgeEffect = new EdgeEffectParameters
                        {
                            Type = EdgeEffectType.Shadow,
                            Offset = new Vector2(0, 50),
                            Hollow = true,
                            Radius = 50,
                            Roundness = 50,
                            Colour = new Color4(0, 255, 255, 255),
                        },
                    });

                    box.Add(box = new InfofulBox
                    {
                        Size = new Vector2(250, 250),
                        Origin = Anchor.Centre,
                        Anchor = Anchor.Centre,
                        Colour = Color4.DarkSeaGreen,
                    });

                    box.OnUpdate += delegate { box.Rotation += 0.05f; };
                    break;
                }

                case 8:
                    TestContainer.Add(new Container
                    {
                        Size = new Vector2(200, 200),
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Children = new Drawable[]
                        {
                            new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Colour = Color4.Gray,
                            },
                            new InfofulBox
                            {
                                RelativeSizeAxes = Axes.Both,
                                Masking = true,
                                CornerRadius = 50,
                                CornerExponent = TestCornerExponent,
                                BorderColour = Color4.Red,
                                BorderThickness = 10,
                                EdgeEffect = new EdgeEffectParameters
                                {
                                    Type = EdgeEffectType.Glow,
                                    Radius = 100,
                                    Roundness = 50,
                                    Colour = Color4.Blue,
                                },
                            }
                        }
                    }.With(c => c.OnLoadComplete += _ =>
                    {
                        c.ResizeWidthTo(-200, 1000, Easing.InOutSine).Then()
                         .ResizeHeightTo(-200, 1000, Easing.InOutSine).Then()
                         .ResizeTo(new Vector2(200, 200), 1000).Loop();
                    }));
                    break;
            }

#if DEBUG
            //if (toggleDebugAutosize.State)
            //    testContainer.Children.FindAll(c => c.HasAutosizeChildren).ForEach(c => c.AutoSizeDebug = true);
#endif
        }

        private class CircularContainerWithInput : CircularContainer
        {
            protected override bool OnHover(HoverEvent e)
            {
                this.ScaleTo(1.2f, 100);
                return true;
            }

            protected override void OnHoverLost(HoverLostEvent e)
            {
                this.ScaleTo(1f, 100);
            }
        }
    }
}
