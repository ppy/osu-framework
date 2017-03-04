﻿// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Transforms;
using osu.Framework.Input;
using OpenTK;
using OpenTK.Graphics;
using osu.Framework.Allocation;
using osu.Framework.Screens.Testing;

namespace osu.Framework.VisualTests.Tests
{
    class TestCaseSizing : TestCase
    {
        public override string Name => @"Size calculations";
        public override string Description => @"Various scenarios which potentially challenge size calculations.";

        private Container testContainer;

        public override void Reset()
        {
            base.Reset();

            Add(testContainer = new Container
            {
                RelativeSizeAxes = Axes.Both,
            });

            string[] testNames =
            {
                @"Multiple children",
                @"Nested children",
                @"AutoSize bench",
                @"RelativeSize bench",
                @"SpriteText 1",
                @"SpriteText 2",
                @"Inverted scaling",
                @"RelativeSize",
                @"Padding",
                @"Margin",
                @"Inner Margin",
                @"Drawable Margin",
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
            testContainer.Clear();

            Container box;

            switch (testType)
            {
                case 0:
                    testContainer.Add(box = new InfofulBoxAutoSize
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre
                    });

                    addCornerMarkers(box);

                    box.Add(new InfofulBox
                    {
                        //chameleon = true,
                        Position = new Vector2(0, 0),
                        Size = new Vector2(25, 25),
                        Origin = Anchor.Centre,
                        Anchor = Anchor.Centre,
                        Colour = Color4.Blue,
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
                case 1:
                    testContainer.Add(box = new InfofulBoxAutoSize
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre
                    });

                    addCornerMarkers(box, 5);


                    box.Add(box = new InfofulBoxAutoSize
                    {
                        Colour = Color4.DarkSeaGreen,
                        Alpha = 0.5f,
                        Origin = Anchor.Centre,
                        Anchor = Anchor.Centre
                    });

                    Drawable localBox = box;
                    box.OnUpdate += delegate { localBox.Rotation += 0.05f; };

                    box.Add(new InfofulBox
                    {
                        //chameleon = true,
                        Size = new Vector2(100, 100),
                        Position = new Vector2(50, 50),
                        Alpha = 0.5f,
                        Origin = Anchor.Centre,
                        Anchor = Anchor.Centre,
                        Colour = Color4.Blue,
                    });
                    break;
                case 2:
                    testContainer.Add(box = new InfofulBoxAutoSize
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre
                    });

                    addCornerMarkers(box, 10, Color4.YellowGreen);

                    for (int i = 0; i < 100; i++)
                    {
                        box.Add(box = new InfofulBoxAutoSize
                        {
                            Colour = new Color4(253, 253, 253, 255),
                            Position = new Vector2(-3, -3),
                            Origin = Anchor.BottomRight,
                            Anchor = Anchor.BottomRight,
                        });
                    }

                    addCornerMarkers(box, 2);

                    box.Add(new InfofulBox
                    {
                        //chameleon = true,
                        Size = new Vector2(50, 50),
                        Origin = Anchor.BottomRight,
                        Anchor = Anchor.BottomRight,
                        Colour = Color4.SeaGreen,
                    });
                    break;
                case 3:
                    testContainer.Add(box = new InfofulBox
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Size = new Vector2(250, 250)
                    });

                    addCornerMarkers(box, 10, Color4.YellowGreen);

                    for (int i = 0; i < 100; i++)
                    {
                        box.Add(box = new InfofulBox
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = new Color4(253, 253, 253, 255),
                            Origin = Anchor.BottomRight,
                            Anchor = Anchor.BottomRight,
                            Size = new Vector2(0.99f, 0.99f)
                        });
                    }

                    addCornerMarkers(box, 2);

                    box.Add(new InfofulBox
                    {
                        //chameleon = true,
                        Size = new Vector2(50, 50),
                        Origin = Anchor.BottomRight,
                        Anchor = Anchor.BottomRight,
                        Colour = Color4.SeaGreen,
                    });
                    break;
                case 4:
                    testContainer.Add(box = new InfofulBoxAutoSize
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.CentreLeft
                    });

                    box.Add(new InfofulBox
                    {
                        Position = new Vector2(5, 0),
                        Size = new Vector2(300, 80),
                        Origin = Anchor.TopLeft,
                        Anchor = Anchor.TopLeft,
                        Colour = Color4.OrangeRed,
                    });

                    box.Add(new SpriteText
                    {
                        Position = new Vector2(5, -20),
                        Text = "Test CentreLeft line 1",
                        Origin = Anchor.CentreLeft,
                        Anchor = Anchor.CentreLeft
                    });

                    box.Add(new SpriteText
                    {
                        Position = new Vector2(5, 20),
                        Text = "Test CentreLeft line 2",
                        Origin = Anchor.CentreLeft,
                        Anchor = Anchor.CentreLeft
                    });
                    break;
                case 5:
                    testContainer.Add(box = new InfofulBoxAutoSize
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.CentreLeft
                    });

                    box.Add(new InfofulBox
                    {
                        Position = new Vector2(5, 0),
                        Size = new Vector2(300, 80),
                        Origin = Anchor.TopLeft,
                        Anchor = Anchor.TopLeft,
                        Colour = Color4.OrangeRed,
                    });

                    box.Add(new SpriteText
                    {
                        Position = new Vector2(5, -20),
                        Text = "123,456,789=",
                        Origin = Anchor.CentreLeft,
                        Anchor = Anchor.CentreLeft,
                        Scale = new Vector2(2f)
                    });

                    box.Add(new SpriteText
                    {
                        Position = new Vector2(5, 20),
                        Text = "123,456,789ms",
                        Origin = Anchor.CentreLeft,
                        Anchor = Anchor.CentreLeft
                    });
                    break;
                case 6:
                    testContainer.Add(box = new InfofulBoxAutoSize
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre
                    });

                    box.Add(box = new InfofulBoxAutoSize
                    {
                        Colour = Color4.OrangeRed,
                        Position = new Vector2(100, 100),
                        Origin = Anchor.Centre,
                        Anchor = Anchor.TopLeft
                    });

                    box.Add(new InfofulBox
                    {
                        Position = new Vector2(100, 100),
                        Size = new Vector2(100, 100),
                        Origin = Anchor.Centre,
                        Anchor = Anchor.TopLeft,
                        Colour = Color4.OrangeRed,
                    });
                    break;
                case 7:
                    Container shrinkContainer;
                    Container<Drawable> boxes;

                    testContainer.Add(shrinkContainer = new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                        Size = new Vector2(0.5f, 1),
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        Children = new Drawable[]
                        {
                            new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Colour = Color4.AliceBlue,
                                Alpha = 0.2f
                            },
                            boxes = new FillFlowContainer {
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                Direction = FillDirection.Down,
                                Spacing = new Vector2(0, 10),
                            }
                        }
                    });

                    for (int i = 0; i < 10; i++)
                    {
                        boxes.Add(new Box
                        {
                            RelativeSizeAxes = Axes.X,
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre,
                            Size = new Vector2(0.9f, 40),
                            Colour = Color4.AliceBlue,
                            Alpha = 0.2f
                        });
                    }

                    shrinkContainer.ScaleTo(new Vector2(1.5f, 1), 1000);
                    shrinkContainer.Delay(1000);
                    shrinkContainer.ScaleTo(new Vector2(1f, 1), 1000);
                    shrinkContainer.Delay(1000);
                    shrinkContainer.Loop();
                    break;

                case 8:
                    {
                        Container box1;
                        Container box2;
                        Container box3;

                        testContainer.Add(new FillFlowContainer
                        {
                            RelativeSizeAxes = Axes.Both,
                            Children = new Drawable[]
                            {
                                // This first guy is used for spacing.
                                new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Size = new Vector2(0.125f, 1),
                                },
                                new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Size = new Vector2(0.25f, 1),
                                    Children = new[]
                                    {
                                        new InfofulBoxAutoSize
                                        {
                                            Anchor = Anchor.Centre,
                                            Origin = Anchor.Centre,
                                            Children = new[]
                                            {
                                                new Container
                                                {
                                                    AutoSizeAxes = Axes.Both,
                                                    Depth = -1,
                                                    Padding = new MarginPadding(50),
                                                    Children = new Drawable[]
                                                    {
                                                        box1 = new InfofulBox
                                                        {
                                                            Anchor = Anchor.TopLeft,
                                                            Origin = Anchor.TopLeft,
                                                            Size = new Vector2(50),
                                                            Colour = Color4.Blue,
                                                        },
                                                    }
                                                }
                                            }
                                        },
                                    }
                                },
                                new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Size = new Vector2(0.25f, 1),
                                    Children = new[]
                                    {
                                        new InfofulBoxAutoSize
                                        {
                                            Anchor = Anchor.Centre,
                                            Origin = Anchor.Centre,
                                            Children = new[]
                                            {
                                                new Container
                                                {
                                                    AutoSizeAxes = Axes.Both,
                                                    Depth = -1,
                                                    Padding = new MarginPadding(50),
                                                    Children = new Drawable[]
                                                    {
                                                        box2 = new InfofulBox
                                                        {
                                                            Anchor = Anchor.Centre,
                                                            Origin = Anchor.Centre,
                                                            Size = new Vector2(50),
                                                            Colour = Color4.Blue,
                                                        },
                                                    }
                                                }
                                            }
                                        },
                                    }
                                },
                                new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Size = new Vector2(0.25f, 1),
                                    Children = new[]
                                    {
                                        new InfofulBoxAutoSize
                                        {
                                            Anchor = Anchor.Centre,
                                            Origin = Anchor.Centre,
                                            Children = new[]
                                            {
                                                new Container
                                                {
                                                    AutoSizeAxes = Axes.Both,
                                                    Depth = -1,
                                                    Padding = new MarginPadding(50),
                                                    Children = new Drawable[]
                                                    {
                                                        box3 = new InfofulBox
                                                        {
                                                            Anchor = Anchor.BottomRight,
                                                            Origin = Anchor.BottomRight,
                                                            Size = new Vector2(50),
                                                            Colour = Color4.Blue,
                                                        },
                                                    }
                                                }
                                            }
                                        },
                                    }
                                },
                            }
                        });

                        foreach (Container b in new[] { box1, box2, box3 })
                        {
                            b.ScaleTo(new Vector2(2, 2), 1000);
                            b.Delay(1000);
                            b.ScaleTo(new Vector2(1, 1), 1000);
                            b.Delay(1000);
                            b.Loop();
                        }

                        break;
                    }

                case 9:
                    {
                        Container box1;
                        Container box2;
                        Container box3;

                        testContainer.Add(new FillFlowContainer
                        {
                            RelativeSizeAxes = Axes.Both,
                            Children = new Drawable[]
                            {
                                // This first guy is used for spacing.
                                new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Size = new Vector2(0.125f, 1),
                                },
                                new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Size = new Vector2(0.25f, 1),
                                    Children = new[]
                                    {
                                        new InfofulBoxAutoSize
                                        {
                                            Anchor = Anchor.Centre,
                                            Origin = Anchor.Centre,
                                            Children = new[]
                                            {
                                                new Container
                                                {
                                                    AutoSizeAxes = Axes.Both,
                                                    Depth = -1,
                                                    Margin = new MarginPadding(50),
                                                    Children = new Drawable[]
                                                    {
                                                        box1 = new InfofulBox
                                                        {
                                                            Anchor = Anchor.TopLeft,
                                                            Origin = Anchor.TopLeft,
                                                            Size = new Vector2(50),
                                                            Colour = Color4.Blue,
                                                        },
                                                    }
                                                }
                                            }
                                        },
                                    }
                                },
                                new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Size = new Vector2(0.25f, 1),
                                    Children = new[]
                                    {
                                        new InfofulBoxAutoSize
                                        {
                                            Anchor = Anchor.Centre,
                                            Origin = Anchor.Centre,
                                            Children = new[]
                                            {
                                                new Container
                                                {
                                                    AutoSizeAxes = Axes.Both,
                                                    Depth = -1,
                                                    Margin = new MarginPadding(50),
                                                    Children = new Drawable[]
                                                    {
                                                        box2 = new InfofulBox
                                                        {
                                                            Anchor = Anchor.Centre,
                                                            Origin = Anchor.Centre,
                                                            Size = new Vector2(50),
                                                            Colour = Color4.Blue,
                                                        },
                                                    }
                                                }
                                            }
                                        },
                                    }
                                },
                                new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Size = new Vector2(0.25f, 1),
                                    Children = new[]
                                    {
                                        new InfofulBoxAutoSize
                                        {
                                            Anchor = Anchor.Centre,
                                            Origin = Anchor.Centre,
                                            Children = new[]
                                            {
                                                new Container
                                                {
                                                    AutoSizeAxes = Axes.Both,
                                                    Depth = -1,
                                                    Margin = new MarginPadding(50),
                                                    Children = new Drawable[]
                                                    {
                                                        box3 = new InfofulBox
                                                        {
                                                            Anchor = Anchor.BottomRight,
                                                            Origin = Anchor.BottomRight,
                                                            Size = new Vector2(50),
                                                            Colour = Color4.Blue,
                                                        },
                                                    }
                                                }
                                            }
                                        },
                                    }
                                },
                            }
                        });

                        foreach (Container b in new[] { box1, box2, box3 })
                        {
                            b.ScaleTo(new Vector2(2, 2), 1000);
                            b.Delay(1000);
                            b.ScaleTo(new Vector2(1, 1), 1000);
                            b.Delay(1000);
                            b.Loop();
                        }

                        break;
                    }

                case 10:
                    {
                        Container box1;
                        Container box2;
                        Container box3;

                        testContainer.Add(new FillFlowContainer
                        {
                            RelativeSizeAxes = Axes.Both,
                            Children = new Drawable[]
                            {
                                // This first guy is used for spacing.
                                new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Size = new Vector2(0.125f, 1),
                                },
                                new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Size = new Vector2(0.25f, 1),
                                    Children = new[]
                                    {
                                        new InfofulBoxAutoSize
                                        {
                                            Anchor = Anchor.Centre,
                                            Origin = Anchor.Centre,
                                            Children = new[]
                                            {
                                                new Container
                                                {
                                                    AutoSizeAxes = Axes.Both,
                                                    Depth = -1,
                                                    Children = new Drawable[]
                                                    {
                                                        box1 = new InfofulBox
                                                        {
                                                            Margin = new MarginPadding(50),
                                                            Anchor = Anchor.TopLeft,
                                                            Origin = Anchor.TopLeft,
                                                            Size = new Vector2(50),
                                                            Colour = Color4.Blue,
                                                        },
                                                    }
                                                }
                                            }
                                        },
                                    }
                                },
                                new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Size = new Vector2(0.25f, 1),
                                    Children = new[]
                                    {
                                        new InfofulBoxAutoSize
                                        {
                                            Anchor = Anchor.Centre,
                                            Origin = Anchor.Centre,
                                            Children = new[]
                                            {
                                                new Container
                                                {
                                                    AutoSizeAxes = Axes.Both,
                                                    Depth = -1,
                                                    Children = new Drawable[]
                                                    {
                                                        box2 = new InfofulBox
                                                        {
                                                            Margin = new MarginPadding(50),
                                                            Anchor = Anchor.Centre,
                                                            Origin = Anchor.Centre,
                                                            Size = new Vector2(50),
                                                            Colour = Color4.Blue,
                                                        },
                                                    }
                                                }
                                            }
                                        },
                                    }
                                },
                                new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Size = new Vector2(0.25f, 1),
                                    Children = new[]
                                    {
                                        new InfofulBoxAutoSize
                                        {
                                            Anchor = Anchor.Centre,
                                            Origin = Anchor.Centre,
                                            Children = new[]
                                            {
                                                new Container
                                                {
                                                    AutoSizeAxes = Axes.Both,
                                                    Depth = -1,
                                                    Children = new Drawable[]
                                                    {
                                                        box3 = new InfofulBox
                                                        {
                                                            Margin = new MarginPadding(50),
                                                            Anchor = Anchor.BottomRight,
                                                            Origin = Anchor.BottomRight,
                                                            Size = new Vector2(50),
                                                            Colour = Color4.Blue,
                                                        },
                                                    }
                                                }
                                            }
                                        },
                                    }
                                },
                            }
                        });

                        foreach (Container b in new[] { box1, box2, box3 })
                        {
                            b.ScaleTo(new Vector2(2, 2), 1000);
                            b.Delay(1000);
                            b.ScaleTo(new Vector2(1, 1), 1000);
                            b.Delay(1000);
                            b.Loop();
                        }

                        break;
                    }

                case 11:
                    {
                        Drawable box1;
                        Drawable box2;
                        Drawable box3;

                        testContainer.Add(new FillFlowContainer
                        {
                            RelativeSizeAxes = Axes.Both,
                            Children = new Drawable[]
                            {
                                // This first guy is used for spacing.
                                new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Size = new Vector2(0.125f, 1),
                                },
                                new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Size = new Vector2(0.25f, 1),
                                    Children = new[]
                                    {
                                        new InfofulBoxAutoSize
                                        {
                                            Anchor = Anchor.Centre,
                                            Origin = Anchor.Centre,
                                            Children = new[]
                                            {
                                                new Container
                                                {
                                                    AutoSizeAxes = Axes.Both,
                                                    Depth = -1,
                                                    Children = new[]
                                                    {
                                                        box1 = new Box
                                                        {
                                                            Margin = new MarginPadding(50),
                                                            Anchor = Anchor.TopLeft,
                                                            Origin = Anchor.TopLeft,
                                                            Size = new Vector2(50),
                                                            Colour = Color4.Blue,
                                                        },
                                                    }
                                                }
                                            }
                                        },
                                    }
                                },
                                new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Size = new Vector2(0.25f, 1),
                                    Children = new[]
                                    {
                                        new InfofulBoxAutoSize
                                        {
                                            Anchor = Anchor.Centre,
                                            Origin = Anchor.Centre,
                                            Children = new[]
                                            {
                                                new Container
                                                {
                                                    AutoSizeAxes = Axes.Both,
                                                    Depth = -1,
                                                    Children = new[]
                                                    {
                                                        box2 = new Box
                                                        {
                                                            Margin = new MarginPadding(50),
                                                            Anchor = Anchor.Centre,
                                                            Origin = Anchor.Centre,
                                                            Size = new Vector2(50),
                                                            Colour = Color4.Blue,
                                                        },
                                                    }
                                                }
                                            }
                                        },
                                    }
                                },
                                new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Size = new Vector2(0.25f, 1),
                                    Children = new[]
                                    {
                                        new InfofulBoxAutoSize
                                        {
                                            Anchor = Anchor.Centre,
                                            Origin = Anchor.Centre,
                                            Children = new[]
                                            {
                                                new Container
                                                {
                                                    AutoSizeAxes = Axes.Both,
                                                    Depth = -1,
                                                    Children = new[]
                                                    {
                                                        box3 = new Box
                                                        {
                                                            Margin = new MarginPadding(50),
                                                            Anchor = Anchor.BottomRight,
                                                            Origin = Anchor.BottomRight,
                                                            Size = new Vector2(50),
                                                            Colour = Color4.Blue,
                                                        },
                                                    }
                                                }
                                            }
                                        },
                                    }
                                },
                            }
                        });

                        foreach (Drawable b in new[] { box1, box2, box3 })
                        {
                            b.ScaleTo(new Vector2(2, 2), 1000);
                            b.Delay(1000);
                            b.ScaleTo(new Vector2(1, 1), 1000);
                            b.Delay(1000);
                            b.Loop();
                        }

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
            box.Add(new InfofulBox
            {
                //chameleon = true,
                Size = new Vector2(size, size),
                Origin = Anchor.TopLeft,
                Anchor = Anchor.TopLeft,
                AllowDrag = false,
                Depth = -2,
                Colour = colour ?? Color4.Red,
            });

            box.Add(new InfofulBox
            {
                //chameleon = true,
                Size = new Vector2(size, size),
                Origin = Anchor.TopRight,
                Anchor = Anchor.TopRight,
                AllowDrag = false,
                Depth = -2,
                Colour = colour ?? Color4.Red,
            });

            box.Add(new InfofulBox
            {
                //chameleon = true,
                Size = new Vector2(size, size),
                Origin = Anchor.BottomLeft,
                Anchor = Anchor.BottomLeft,
                AllowDrag = false,
                Depth = -2,
                Colour = colour ?? Color4.Red,
            });

            box.Add(new InfofulBox
            {
                //chameleon = true,
                Size = new Vector2(size, size),
                Origin = Anchor.BottomRight,
                Anchor = Anchor.BottomRight,
                AllowDrag = false,
                Depth = -2,
                Colour = colour ?? Color4.Red,
            });
        }
    }

    class InfofulBoxAutoSize : Container
    {
        [BackgroundDependencyLoader]
        private void load(Game game)
        {
            AutoSizeAxes = Axes.Both;

            Masking = true;

            Add(new Box
            {
                RelativeSizeAxes = Axes.Both,
                Depth = float.MaxValue,
            });
        }

        public bool AllowDrag = true;

        protected override bool OnDrag(InputState state)
        {
            if (!AllowDrag) return false;

            Position += state.Mouse.Delta;
            return true;
        }

        protected override bool OnDragEnd(InputState state)
        {
            return true;
        }

        protected override bool OnDragStart(InputState state) => AllowDrag;
    }

    class InfofulBox : Container
    {
        private SpriteText debugInfo;

        public bool Chameleon = false;
        public bool AllowDrag = true;

        protected override bool OnDrag(InputState state)
        {
            if (!AllowDrag) return false;

            Position += state.Mouse.Delta;
            return true;
        }

        protected override bool OnDragEnd(InputState state)
        {
            return true;
        }

        protected override bool OnDragStart(InputState state) => AllowDrag;

        public InfofulBox()
        {
            Add(new Box
            {
                RelativeSizeAxes = Axes.Both,
                Depth = float.MaxValue,
            });

            debugInfo = new SpriteText
            {
                Colour = Color4.Black
            };
            Add(debugInfo);
        }

        int lastSwitch;

        protected override void Update()
        {
            if (Chameleon && (int)Time.Current / 1000 != lastSwitch)
            {
                lastSwitch = (int)Time.Current / 1000;
                switch (lastSwitch % 6)
                {
                    case 0:
                        Anchor = (Anchor)((int)Anchor + 1);
                        Origin = (Anchor)((int)Origin + 1);
                        break;
                    case 1:
                        MoveTo(new Vector2(0, 0), 800, EasingTypes.Out);
                        break;
                    case 2:
                        MoveTo(new Vector2(200, 0), 800, EasingTypes.Out);
                        break;
                    case 3:
                        MoveTo(new Vector2(200, 200), 800, EasingTypes.Out);
                        break;
                    case 4:
                        MoveTo(new Vector2(0, 200), 800, EasingTypes.Out);
                        break;
                    case 5:
                        MoveTo(new Vector2(0, 0), 800, EasingTypes.Out);
                        break;
                }
            }

            base.Update();

            //debugInfo.Text = ToString();
        }
    }
}
