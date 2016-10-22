// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Transformations;
using osu.Framework.Input;
using OpenTK;
using OpenTK.Graphics;
using osu.Framework.GameModes.Testing;
using System;

namespace osu.Framework.VisualTests.Tests
{
    class TestCaseMasking : TestCase
    {
        public override string Name => @"Masking";
        public override string Description => @"Various scenarios which potentially challenge masking calculations.";

        private Container testContainer;

        public override void Reset()
        {
            base.Reset();

            Add(testContainer = new Container()
            {
                RelativeSizeAxes = Axes.Both,
            });

            string[] testNames = new[]
            {
                @"Round corner masking",
                @"Edge/border blurriness",
                @"Nested masking",
            };

            for (int i = 0; i < testNames.Length; i++)
            {
                int test = i;
                AddButton(testNames[i], delegate { loadTest(test); });
            }

            loadTest(0);
        }

        private void loadTest(int testType)
        {
            testContainer.Clear();

            switch (testType)
            {
                default:
                case 0:
                    {
                        Color4 glowColour = Color4.Aquamarine;
                        glowColour.A = 0.5f;

                        Container box;
                        testContainer.Add(box = new InfofulBoxAutoSize
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Masking = true,
                            CornerRadius = 100,
                            GlowRadius = 25,
                            BorderColour = Color4.Aquamarine,
                            BorderThickness = 3,
                            GlowColour = glowColour
                        });

                        addCornerMarkers(box);

                        box.Add(new InfofulBox(RectangleF.Empty, 0, Color4.Blue)
                        {
                            Position = new Vector2(0, 0),
                            Size = new Vector2(25, 25),
                            Origin = Anchor.Centre,
                            Anchor = Anchor.Centre
                        });

                        box.Add(box = new InfofulBox(RectangleF.Empty, 0, Color4.DarkSeaGreen)
                        {
                            Size = new Vector2(250, 250),
                            Alpha = 0.5f,
                            Origin = Anchor.Centre,
                            Anchor = Anchor.Centre
                        });

                        box.OnUpdate += delegate { box.Rotation += 0.05f; };
                        break;
                    }

                case 1:
                    {
                        Func<float, Drawable> createMaskingBox = delegate(float scale)
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

                        testContainer.Add(new FlowContainer
                        {
                            RelativeSizeAxes = Axes.Both,
                            Children = new[]
                            {
                                new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Size = new Vector2(0.5f),
                                    Masking = true,
                                    Children = new Drawable[] { createMaskingBox(100) }
                                },
                                new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Size = new Vector2(0.5f),
                                    Masking = true,
                                    Children = new Drawable[] { createMaskingBox(10) }
                                },
                                new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Size = new Vector2(0.5f),
                                    Masking = true,
                                    Children = new Drawable[] { createMaskingBox(1) }
                                },
                                new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Size = new Vector2(0.5f),
                                    Masking = true,
                                    Children = new Drawable[] { createMaskingBox(0.1f) }
                                },
                            }
                        });

                        break;
                    }

                case 2:
                    {
                        testContainer.Add(new Container
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

        private void addCornerMarkers(Container box, int size = 50, Color4? colour = null)
        {
            box.Add(new InfofulBox(RectangleF.Empty, 2, colour ?? Color4.Red)
            {
                //chameleon = true,
                Size = new Vector2(size, size),
                Origin = Anchor.TopLeft,
                Anchor = Anchor.TopLeft,
                AllowDrag = false
            });

            box.Add(new InfofulBox(RectangleF.Empty, 2, colour ?? Color4.Red)
            {
                //chameleon = true,
                Size = new Vector2(size, size),
                Origin = Anchor.TopRight,
                Anchor = Anchor.TopRight,
                AllowDrag = false
            });

            box.Add(new InfofulBox(RectangleF.Empty, 2, colour ?? Color4.Red)
            {
                //chameleon = true,
                Size = new Vector2(size, size),
                Origin = Anchor.BottomLeft,
                Anchor = Anchor.BottomLeft,
                AllowDrag = false
            });

            box.Add(new InfofulBox(RectangleF.Empty, 2, colour ?? Color4.Red)
            {
                //chameleon = true,
                Size = new Vector2(size, size),
                Origin = Anchor.BottomRight,
                Anchor = Anchor.BottomRight,
                AllowDrag = false
            });
        }
    }

}
