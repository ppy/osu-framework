// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using OpenTK;
using OpenTK.Graphics;
using osu.Framework.GameModes.Testing;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics.Colour;

namespace osu.Framework.VisualTests.Tests
{
    class TestCaseColourGradient : TestCase
    {
        public override string Name => @"Colour Gradient";
        public override string Description => @"Various cases of colour gradients.";

        private Box[] boxes = new Box[4];

        public override void Reset()
        {
            base.Reset();

            Color4 transparentBlack = new Color4(0, 0, 0, 0);

            ColourInfo[] colours = new[]
            {
                new ColourInfo()
                {
                    TopLeft = Color4.White,
                    BottomLeft = Color4.Blue,
                    TopRight = Color4.Red,
                    BottomRight = Color4.Green,
                },
                new ColourInfo()
                {
                    TopLeft = Color4.White,
                    BottomLeft = Color4.White,
                    TopRight = Color4.Black,
                    BottomRight = Color4.Black,
                },
                new ColourInfo()
                {
                    TopLeft = Color4.White,
                    BottomLeft = Color4.White,
                    TopRight = Color4.Transparent,
                    BottomRight = Color4.Transparent,
                },
                new ColourInfo()
                {
                    TopLeft = Color4.White,
                    BottomLeft = Color4.White,
                    TopRight = transparentBlack,
                    BottomRight = transparentBlack,
                },
            };

            Add(new Container()
            {
                RelativeSizeAxes = Axes.Both,
                Children = new[]
                {
                    new FlowContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        Children = new[]
                        {
                            new Container
                            {
                                RelativeSizeAxes = Axes.Both,
                                Size = new Vector2(0.5f),
                                Children = new Drawable[]
                                {
                                    new SpriteText
                                    {
                                        Text = "Colours",
                                        TextSize = 20,
                                        ColourInfo = colours[0],
                                    },
                                    boxes[0] = new Box
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Anchor = Anchor.Centre,
                                        Origin = Anchor.Centre,
                                        Size = new Vector2(0.5f),
                                        ColourInfo = colours[0],
                                    }
                                }
                            },
                            new Container
                            {
                                RelativeSizeAxes = Axes.Both,
                                Size = new Vector2(0.5f),
                                Children = new Drawable[]
                                {
                                    new SpriteText
                                    {
                                        Text = "White to black (linear brightness gradient)",
                                        TextSize = 20,
                                        ColourInfo = colours[0],
                                    },
                                    boxes[1] = new Box
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Colour = Color4.White,
                                        Anchor = Anchor.Centre,
                                        Origin = Anchor.Centre,
                                        Size = new Vector2(0.5f),
                                        ColourInfo = colours[1],
                                    }
                                }
                            },
                            new Container
                            {
                                RelativeSizeAxes = Axes.Both,
                                Size = new Vector2(0.5f),
                                Children = new Drawable[]
                                {
                                    new SpriteText
                                    {
                                        Text = "White to transparent white (sRGB brightness gradient)",
                                        TextSize = 20,
                                        ColourInfo = colours[0],
                                    },
                                    boxes[2] = new Box
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Colour = Color4.White,
                                        Anchor = Anchor.Centre,
                                        Origin = Anchor.Centre,
                                        Size = new Vector2(0.5f),
                                        ColourInfo = colours[2],
                                    }
                                }
                            },
                            new Container
                            {
                                RelativeSizeAxes = Axes.Both,
                                Size = new Vector2(0.5f),
                                Children = new Drawable[]
                                {
                                    new SpriteText
                                    {
                                        Text = "White to transparent black (mixed brightness gradient)",
                                        TextSize = 20,
                                        ColourInfo = colours[0],
                                    },
                                    boxes[3] = new Box
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Colour = Color4.White,
                                        Anchor = Anchor.Centre,
                                        Origin = Anchor.Centre,
                                        Size = new Vector2(0.5f),
                                        ColourInfo = colours[3],
                                    }
                                }
                            },
                        }
                    }
                }
            });
        }

        protected override void Update()
        {
            base.Update();

            foreach (Drawable box in boxes)
                box.Rotation += 0.01f;
        }
    }
}
