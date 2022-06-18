// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using NUnit.Framework;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osu.Framework.Utils;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Containers
{
    [System.ComponentModel.Description("potentially challenging size calculations")]
    public class TestSceneSizing : FrameworkTestScene
    {
        private Container testContainer;

        public TestSceneSizing()
        {
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
                @"Relative Inside Autosize",
                @"Negative sizing"
            };

            for (int i = 0; i < testNames.Length; i++)
            {
                int test = i;
                AddStep(testNames[i], delegate { loadTest(test); });
            }
        }

        [Test]
        public void TestBypassAutoSizeAxes()
        {
            const float autosize_height = 300;

            Container autoSizeContainer = null;
            Box boxSizeReference = null;

            AddStep("init", () =>
            {
                Child = autoSizeContainer = new Container
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Width = 300,
                    AutoSizeAxes = Axes.Y,
                    Children = new Drawable[]
                    {
                        new Box
                        {
                            Colour = Color4.Green,
                            RelativeSizeAxes = Axes.Both
                        },
                        boxSizeReference = new Box
                        {
                            RelativeSizeAxes = Axes.X,
                            Height = autosize_height,
                            Colour = Color4.Red.Opacity(0.2f),
                        }
                    }
                };
            });
            AddAssert($"height = {autosize_height}", () => Precision.AlmostEquals(autosize_height, autoSizeContainer.DrawHeight));
            AddStep("bypass y", () => boxSizeReference.BypassAutoSizeAxes = Axes.Y);
            AddAssert("height = 0", () => Precision.AlmostEquals(0, autoSizeContainer.DrawHeight));
            AddStep("bypass none", () => boxSizeReference.BypassAutoSizeAxes = Axes.None);
            AddAssert($"height = {autosize_height}", () => Precision.AlmostEquals(autosize_height, autoSizeContainer.DrawHeight));
        }

        [Test]
        public void TestParentInvalidations()
        {
            Container child = null;
            Vector2 initialSize = Vector2.Zero;

            AddStep("add child", () =>
            {
                Child = child = new Container
                {
                    AutoSizeAxes = Axes.Both,
                    Child = new Container
                    {
                        // These two properties are important, as they make the parent's autosize 0
                        Anchor = Anchor.TopLeft,
                        Origin = Anchor.BottomRight,
                        AutoSizeAxes = Axes.Both,
                        Child = new Box { Size = new Vector2(100) },
                    }
                };

                // Should already be 0
                initialSize = child.Size;
            });

            // Upon LoadComplete(), one invalidation occurs which causes autosize to recompute
            // Since nothing has changed since the previous frame, the size of the child should remain the same
            AddAssert("size is the same after one frame", () => child.Size == initialSize);
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
            Child = testContainer = new Container
            {
                RelativeSizeAxes = Axes.Both,
            };
            addCrosshair();

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

                    for (int i = 0; i < 40; i++)
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
                            boxes = new FillFlowContainer
                            {
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                Direction = FillDirection.Vertical,
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

                    shrinkContainer.ScaleTo(new Vector2(1.5f, 1), 1000).Then().ScaleTo(Vector2.One, 1000).Loop();
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
                        b.ScaleTo(new Vector2(2), 1000).Then().ScaleTo(Vector2.One, 1000).Loop();

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
                        b.ScaleTo(new Vector2(2), 1000).Then().ScaleTo(Vector2.One, 1000).Loop();

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
                        b.ScaleTo(new Vector2(2), 1000).Then().ScaleTo(Vector2.One, 1000).Loop();

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
                        b.ScaleTo(new Vector2(2), 1000).Then().ScaleTo(Vector2.One, 1000).Loop();

                    break;
                }

                case 12:
                {
                    // demonstrates how relativeaxes drawables act inside an autosize parent
                    Drawable sizedBox;

                    testContainer.Add(new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        Children = new Drawable[]
                        {
                            new Container
                            {
                                Size = new Vector2(300),
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre,
                                Children = new Drawable[]
                                {
                                    new Box { Colour = Color4.Gray, RelativeSizeAxes = Axes.Both },
                                    new Container
                                    {
                                        AutoSizeAxes = Axes.Both,
                                        Anchor = Anchor.Centre,
                                        Origin = Anchor.Centre,
                                        Children = new[]
                                        {
                                            // defines the size of autosize
                                            sizedBox = new Box
                                            {
                                                Colour = Color4.Red,
                                                Anchor = Anchor.Centre,
                                                Origin = Anchor.Centre,
                                                Size = new Vector2(100f)
                                            },
                                            // gets relative size based on autosize
                                            new Box
                                            {
                                                Colour = Color4.Black,
                                                RelativeSizeAxes = Axes.Both,
                                                Size = new Vector2(0.5f)
                                            },
                                        }
                                    }
                                }
                            }
                        }
                    });

                    sizedBox.ScaleTo(new Vector2(2), 1000, Easing.Out).Then().ScaleTo(Vector2.One, 1000, Easing.In).Loop();
                    break;
                }

                case 13:
                {
                    testContainer.Add(new FillFlowContainer
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Vertical,
                        Spacing = new Vector2(0, 20),
                        Children = new[]
                        {
                            new FillFlowContainer
                            {
                                Name = "Top row",
                                RelativeSizeAxes = Axes.X,
                                Height = 200,
                                Direction = FillDirection.Horizontal,
                                Spacing = new Vector2(100, 0),
                                Children = new[]
                                {
                                    new NegativeSizingContainer(Anchor.TopLeft, true),
                                    new NegativeSizingContainer(Anchor.Centre, true),
                                    new NegativeSizingContainer(Anchor.BottomRight, true)
                                }
                            },
                            new FillFlowContainer
                            {
                                Name = "Bottom row",
                                RelativeSizeAxes = Axes.X,
                                Height = 200,
                                Direction = FillDirection.Horizontal,
                                Spacing = new Vector2(100, 0),
                                Children = new[]
                                {
                                    new NegativeSizingContainer(Anchor.TopLeft, false),
                                    new NegativeSizingContainer(Anchor.Centre, false),
                                    new NegativeSizingContainer(Anchor.BottomRight, false)
                                }
                            }
                        }
                    });

                    break;
                }
            }
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

        private class NegativeSizingContainer : Container
        {
            private const float size = 200;

            private readonly Box box;
            private readonly SpriteText text;

            private readonly bool useScale;

            public NegativeSizingContainer(Anchor anchor, bool useScale)
            {
                this.useScale = useScale;

                Anchor = Anchor.Centre;
                Origin = Anchor.Centre;
                AutoSizeAxes = Axes.Both;

                Children = new Drawable[]
                {
                    box = new Box
                    {
                        Anchor = anchor,
                        Origin = anchor,
                        Size = new Vector2(size)
                    },
                    text = new SpriteText
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Colour = Color4.Red,
                        BypassAutoSizeAxes = Axes.Both
                    }
                };
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();

                if (useScale)
                    box.ScaleTo(new Vector2(-1), 3000, Easing.InSine).Then().ScaleTo(new Vector2(1), 3000, Easing.InSine).Loop();
                else
                    box.ResizeTo(new Vector2(-size), 3000, Easing.InSine).Then().ResizeTo(new Vector2(size), 3000, Easing.InSine).Loop();
            }

            protected override void Update()
            {
                base.Update();

                text.Text = useScale ? $"Scale: {box.Scale}" : $"Size: {box.Size}";
            }
        }
    }

    internal class InfofulBoxAutoSize : Container
    {
        protected override Container<Drawable> Content { get; }

        public InfofulBoxAutoSize()
        {
            AutoSizeAxes = Axes.Both;

            Masking = true;

            InternalChildren = new Drawable[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Depth = float.MaxValue,
                },
                Content = new Container
                {
                    AutoSizeAxes = Axes.Both,
                }
            };
        }

        public bool AllowDrag = true;

        protected override void OnDrag(DragEvent e)
        {
            if (!AllowDrag) return;

            Position += e.Delta;
        }

        protected override bool OnDragStart(DragStartEvent e) => AllowDrag;
    }

    internal class InfofulBox : Container
    {
        public bool AllowDrag = true;

        protected override void OnDrag(DragEvent e)
        {
            if (!AllowDrag) return;

            Position += e.Delta;
        }

        protected override bool OnDragStart(DragStartEvent e) => AllowDrag;

        public InfofulBox()
        {
            Add(new Box
            {
                RelativeSizeAxes = Axes.Both,
                Depth = float.MaxValue,
            });
        }
    }
}
