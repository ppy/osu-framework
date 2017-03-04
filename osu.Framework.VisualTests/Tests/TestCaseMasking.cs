﻿// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using OpenTK;
using OpenTK.Graphics;
using System;
using osu.Framework.Screens.Testing;

namespace osu.Framework.VisualTests.Tests
{
    class TestCaseMasking : TestCase
    {
        public override string Name => @"Masking";
        public override string Description => @"Various scenarios which potentially challenge masking calculations.";

        protected Container TestContainer;

        public override void Reset()
        {
            base.Reset();

            Add(TestContainer = new Container()
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
            };

            for (int i = 0; i < testNames.Length; i++)
            {
                int test = i;
                AddButton(testNames[i], delegate { loadTest(test); });
            }

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

            switch (testType)
            {
                default:
                    {
                        Color4 glowColour = Color4.Aquamarine;
                        glowColour.A = 0.5f;

                        Container box;
                        TestContainer.Add(box = new InfofulBoxAutoSize
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Masking = true,
                            CornerRadius = 100,
                            BorderColour = Color4.Aquamarine,
                            BorderThickness = 3,
                            EdgeEffect = new EdgeEffect
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
                        Color4 glowColour = Color4.Aquamarine;
                        glowColour.A = 0.5f;

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
                                    Size = new Vector2(400, 400),
                                    Alpha = 0.5f,
                                    Origin = Anchor.Centre,
                                    Anchor = Anchor.Centre,
                                    Colour = Color4.DarkSeaGreen,
                                }
                            }
                        });

                        box.OnUpdate += delegate { box.Rotation += 0.05f; box.CornerRadius = 100 + 100 * (float)Math.Sin(box.Rotation * 0.01); };
                        break;
                    }

                case 2:
                    {
                        Color4 glowColour = Color4.Aquamarine;
                        glowColour.A = 0.5f;

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
                            EdgeEffect = new EdgeEffect
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
                        Func<float, Drawable> createMaskingBox = delegate (float scale)
                        {
                            float size = 200 / scale;
                            return new Container
                            {
                                Masking = true,
                                CornerRadius = 25 / scale,
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
                                        TextSize = 20 / scale,
                                        Colour = Color4.Blue,
                                        Anchor = Anchor.Centre,
                                        Origin = Anchor.Centre,
                                    },
                                }
                            };
                        };

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
                                    Children = new[] { createMaskingBox(100) }
                                },
                                new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Size = new Vector2(0.5f),
                                    Masking = true,
                                    Children = new[] { createMaskingBox(10) }
                                },
                                new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Size = new Vector2(0.5f),
                                    Masking = true,
                                    Children = new[] { createMaskingBox(1) }
                                },
                                new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Size = new Vector2(0.5f),
                                    Masking = true,
                                    Children = new[] { createMaskingBox(0.1f) }
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
            }

#if DEBUG
            //if (toggleDebugAutosize.State)
            //    testContainer.Children.FindAll(c => c.HasAutosizeChildren).ForEach(c => c.AutoSizeDebug = true);
#endif
        }
    }

}
