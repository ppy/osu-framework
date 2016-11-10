// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using OpenTK;
using OpenTK.Graphics;
using osu.Framework.GameModes.Testing;

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
                                    },
                                    boxes[0] = new Box
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Anchor = Anchor.Centre,
                                        Origin = Anchor.Centre,
                                        Size = new Vector2(0.5f),
                                        ColourInfo = new ColourInfo() { TopLeft = Color4.White, BottomLeft = Color4.Blue, TopRight = Color4.Red, BottomRight = Color4.Green },
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
                                        Text = "White to black",
                                        TextSize = 20,
                                    },
                                    boxes[1] = new Box
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Colour = Color4.White,
                                        Anchor = Anchor.Centre,
                                        Origin = Anchor.Centre,
                                        Size = new Vector2(0.5f),
                                        ColourInfo = new ColourInfo() { TopLeft = Color4.White, BottomLeft = Color4.White, TopRight = Color4.Black, BottomRight = Color4.Black },
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
                                        Text = "White to transparent white",
                                        TextSize = 20,
                                    },
                                    boxes[2] = new Box
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Colour = Color4.White,
                                        Anchor = Anchor.Centre,
                                        Origin = Anchor.Centre,
                                        Size = new Vector2(0.5f),
                                        ColourInfo = new ColourInfo() { TopLeft = Color4.White, BottomLeft = Color4.White, TopRight = Color4.Transparent, BottomRight = Color4.Transparent },
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
                                        Text = "White to transparent black",
                                        TextSize = 20,
                                    },
                                    boxes[3] = new Box
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Colour = Color4.White,
                                        Anchor = Anchor.Centre,
                                        Origin = Anchor.Centre,
                                        Size = new Vector2(0.5f),
                                        ColourInfo = new ColourInfo() { TopLeft = Color4.White, BottomLeft = Color4.White, TopRight = transparentBlack, BottomRight = transparentBlack },
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
