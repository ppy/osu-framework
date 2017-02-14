// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using OpenTK;
using OpenTK.Graphics;
using osu.Framework.GameModes.Testing;

namespace osu.Framework.VisualTests.Tests
{
    class TestCaseSmoothedEdges : TestCase
    {
        public override string Name => @"Smoothed Edges";
        public override string Description => @"Boxes with automatically smoothed edges (no anti-aliasing).";

        private Box[] boxes = new Box[4];

        public override void Reset()
        {
            base.Reset();

            Add(new Container
            {
                RelativeSizeAxes = Axes.Both,
                Children = new[]
                {
                    new FlowContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        Children = new []
                        {
                            new Container
                            {
                                RelativeSizeAxes = Axes.Both,
                                Size = new Vector2(0.5f),
                                Children = new Drawable[]
                                {
                                    new SpriteText
                                    {
                                        Text = "No smoothing",
                                        TextSize = 20,
                                    },
                                    boxes[0] = new Box
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Colour = Color4.White,
                                        Anchor = Anchor.Centre,
                                        Origin = Anchor.Centre,
                                        Size = new Vector2(0.5f),
                                        EdgeSmoothness = Vector2.Zero,
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
                                        Text = "2-smoothing perpendicular to Y",
                                        TextSize = 20,
                                    },
                                    boxes[1] = new Box
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Colour = Color4.White,
                                        Anchor = Anchor.Centre,
                                        Origin = Anchor.Centre,
                                        Size = new Vector2(0.5f),
                                        EdgeSmoothness = new Vector2(0, 2),
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
                                        Text = "1-smoothing",
                                        TextSize = 20,
                                    },
                                    boxes[2] = new Box
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Colour = Color4.White,
                                        Anchor = Anchor.Centre,
                                        Origin = Anchor.Centre,
                                        Size = new Vector2(0.5f),
                                        EdgeSmoothness = Vector2.One,
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
                                        Text = "2-smoothing",
                                        TextSize = 20,
                                    },
                                    boxes[3] = new Box
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Colour = Color4.White,
                                        Anchor = Anchor.Centre,
                                        Origin = Anchor.Centre,
                                        Size = new Vector2(0.5f),
                                        EdgeSmoothness = Vector2.One * 2,
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
