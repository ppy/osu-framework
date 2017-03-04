﻿// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
using OpenTK.Graphics;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Containers;
using osu.Framework.Input;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Screens.Testing;

namespace osu.Framework.VisualTests.Tests
{
    class TestCasePadding : TestCase
    {
        public override string Name => @"Padding";

        public override string Description => @"Add fixed padding via a PaddingContainer";

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
                            Masking = true,
                            Children = new Drawable[]
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
                            }
                        },
                        new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Size = new Vector2(0.5f),
                            Masking = true,
                            Children = new Drawable[]
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
                            }
                        },
                        new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Size = new Vector2(0.5f),
                            Masking = true,
                            Children = new Drawable[]
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
                            }
                        },
                        new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Size = new Vector2(0.5f),
                            Masking = true,
                            Children = new Drawable[]
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
                            }
                        },
                    }
                }
            };
        }

        class PaddedBox : Container
        {
            private SpriteText t1, t2, t3, t4;

            Container content;

            protected override Container<Drawable> Content => content;

            public PaddedBox(Color4 colour)
            {
                AddInternal(new Drawable[]
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
    }
}
