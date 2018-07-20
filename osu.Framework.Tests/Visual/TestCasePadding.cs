﻿// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.States;
using osu.Framework.Testing;
using OpenTK;
using OpenTK.Graphics;

namespace osu.Framework.Tests.Visual
{
    public class TestCasePadding : GridTestCase
    {
        public TestCasePadding() : base(2, 2)
        {
            Cell(0).AddRange(new Drawable[]
            {
                new SpriteText { Text = @"Padding - 20 All Sides" },
                new Container
                {
                    AutoSizeAxes = Axes.Both,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Children = new Drawable[]
                    {
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = Color4.White,
                        },
                        new PaddedBox(Color4.Blue)
                        {
                            Padding = new MarginPadding(20),
                            Size = new Vector2(200),
                            Origin = Anchor.Centre,
                            Anchor = Anchor.Centre,
                            Masking = true,
                            Children = new Drawable[]
                            {
                                new PaddedBox(Color4.DarkSeaGreen)
                                {
                                    Padding = new MarginPadding(40),
                                    RelativeSizeAxes = Axes.Both,
                                    Origin = Anchor.Centre,
                                    Anchor = Anchor.Centre
                                }
                            }
                        }
                    }
                }
            });

            Cell(1).AddRange(new Drawable[]
            {
                new SpriteText { Text = @"Padding - 20 Top, Left" },
                new Container
                {
                    AutoSizeAxes = Axes.Both,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Children = new Drawable[]
                    {
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = Color4.White,
                        },
                        new PaddedBox(Color4.Blue)
                        {
                            Padding = new MarginPadding
                            {
                                Top = 20,
                                Left = 20,
                            },
                            Size = new Vector2(200),
                            Origin = Anchor.Centre,
                            Anchor = Anchor.Centre,
                            Masking = true,
                            Children = new Drawable[]
                            {
                                new PaddedBox(Color4.DarkSeaGreen)
                                {
                                    Padding = new MarginPadding(40),
                                    RelativeSizeAxes = Axes.Both,
                                    Origin = Anchor.Centre,
                                    Anchor = Anchor.Centre
                                }
                            }
                        }
                    }
                }
            });

            Cell(2).AddRange(new Drawable[]
            {
                new SpriteText { Text = @"Margin - 20 All Sides" },
                new Container
                {
                    AutoSizeAxes = Axes.Both,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Children = new Drawable[]
                    {
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = Color4.White,
                        },
                        new PaddedBox(Color4.Blue)
                        {
                            Margin = new MarginPadding(20),
                            Size = new Vector2(200),
                            Origin = Anchor.Centre,
                            Anchor = Anchor.Centre,
                            Masking = true,
                            Children = new Drawable[]
                            {
                                new PaddedBox(Color4.DarkSeaGreen)
                                {
                                    Padding = new MarginPadding(20),
                                    RelativeSizeAxes = Axes.Both,
                                    Origin = Anchor.Centre,
                                    Anchor = Anchor.Centre
                                }
                            }
                        }
                    }
                }
            });

            Cell(3).AddRange(new Drawable[]
            {
                new SpriteText { Text = @"Margin - 20 Top, Left" },
                new Container
                {
                    AutoSizeAxes = Axes.Both,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Children = new Drawable[]
                    {
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = Color4.White,
                        },
                        new PaddedBox(Color4.Blue)
                        {
                            Margin = new MarginPadding
                            {
                                Top = 20,
                                Left = 20,
                            },
                            Size = new Vector2(200),
                            Origin = Anchor.Centre,
                            Anchor = Anchor.Centre,
                            Masking = true,
                            Children = new Drawable[]
                            {
                                new PaddedBox(Color4.DarkSeaGreen)
                                {
                                    Padding = new MarginPadding(40),
                                    RelativeSizeAxes = Axes.Both,
                                    Origin = Anchor.Centre,
                                    Anchor = Anchor.Centre
                                }
                            }
                        }
                    }
                }
            });
        }

        private class PaddedBox : Container
        {
            private readonly SpriteText t1;
            private readonly SpriteText t2;
            private readonly SpriteText t3;
            private readonly SpriteText t4;

            private readonly Container content;

            protected override Container<Drawable> Content => content;

            public PaddedBox(Color4 colour)
            {
                AddRangeInternal(new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = colour,
                    },
                    content = new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                    },
                    t1 = new SpriteText
                    {
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre
                    },
                    t2 = new SpriteText
                    {
                        Rotation = 90,
                        Anchor = Anchor.CentreRight,
                        Origin = Anchor.TopCentre
                    },
                    t3 = new SpriteText
                    {
                        Anchor = Anchor.BottomCentre,
                        Origin = Anchor.BottomCentre
                    },
                    t4 = new SpriteText
                    {
                        Rotation = -90,
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.TopCentre
                    }
                });

                Masking = true;
            }

            public override bool Invalidate(Invalidation invalidation = Invalidation.All, Drawable source = null, bool shallPropagate = true)
            {
                t1.Text = (Padding.Top > 0 ? $"p{Padding.Top}" : string.Empty) + (Margin.Top > 0 ? $"m{Margin.Top}" : string.Empty);
                t2.Text = (Padding.Right > 0 ? $"p{Padding.Right}" : string.Empty) + (Margin.Right > 0 ? $"m{Margin.Right}" : string.Empty);
                t3.Text = (Padding.Bottom > 0 ? $"p{Padding.Bottom}" : string.Empty) + (Margin.Bottom > 0 ? $"m{Margin.Bottom}" : string.Empty);
                t4.Text = (Padding.Left > 0 ? $"p{Padding.Left}" : string.Empty) + (Margin.Left > 0 ? $"m{Margin.Left}" : string.Empty);

                return base.Invalidate(invalidation, source, shallPropagate);
            }

            protected override bool OnDrag(InputState state)
            {
                Position += state.Mouse.Delta;
                return true;
            }

            protected override bool OnDragEnd(InputState state) => true;

            protected override bool OnDragStart(InputState state) => true;
        }
    }
}
