// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
using OpenTK.Graphics;
using osu.Framework.Graphics;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Containers;
using osu.Framework.Input;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Screens.Testing;

namespace osu.Framework.VisualTests.Tests
{
    class TestCaseFillModes : TestCase
    {
        public override string Name => @"Sprites - FillModes";

        public override string Description => @"Test sprite display and fill modes";

        Texture sampleTexture;

        [BackgroundDependencyLoader]
        private void load(TextureStore store)
        {
            sampleTexture = store.Get(@"sample-texture");
        }

        public override void Reset()
        {
            base.Reset();

            Children = new Drawable[]
            {
                new FlowContainer
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
                                new SpriteText { Text = @"FillMode.None" },
                                new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Size = new Vector2(0.5f),
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.Centre,
                                    Masking = true,
                                    Children = new Drawable[]
                                    {
                                        new Box
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                            Colour = Color4.Blue,
                                        },
                                        new Sprite
                                        {
                                            Anchor = Anchor.Centre,
                                            Origin = Anchor.Centre,
                                            Texture = sampleTexture
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
                                new SpriteText { Text = @"FillMode.Stretch" },
                                new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Size = new Vector2(0.5f),
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.Centre,
                                    Masking = true,
                                    Children = new Drawable[]
                                    {
                                        new Box
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                            Colour = Color4.Blue,
                                        },
                                        new Sprite
                                        {
                                            FillMode = FillMode.Stretch,
                                            Anchor = Anchor.Centre,
                                            Origin = Anchor.Centre,
                                            Texture = sampleTexture
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
                                new SpriteText { Text = @"FillMode.Fill" },
                                new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Size = new Vector2(0.5f),
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.Centre,
                                    Masking = true,
                                    Children = new Drawable[]
                                    {
                                        new Box
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                            Colour = Color4.Blue,
                                        },
                                        new Sprite
                                        {
                                            FillMode = FillMode.Fill,
                                            Anchor = Anchor.Centre,
                                            Origin = Anchor.Centre,
                                            Texture = sampleTexture
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
                                new SpriteText { Text = @"FillMode.Fit" },
                                new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Size = new Vector2(0.5f),
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.Centre,
                                    Masking = true,
                                    Children = new Drawable[]
                                    {
                                        new Box
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                            Colour = Color4.Blue,
                                        },
                                        new Sprite
                                        {
                                            FillMode = FillMode.Fit,
                                            Anchor = Anchor.Centre,
                                            Origin = Anchor.Centre,
                                            Texture = sampleTexture
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
