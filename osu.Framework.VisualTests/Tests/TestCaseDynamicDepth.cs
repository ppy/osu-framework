// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
using OpenTK.Graphics;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.MathUtils;
using osu.Framework.Testing;

namespace osu.Framework.VisualTests.Tests
{
    public class TestCaseDynamicDepth : TestCase
    {
        public override string Description => @"Dynamically change depth of a child.";

        private DepthBox red;
        private DepthBox blue;
        private DepthBox green;
        private DepthBox purple;

        public override void Reset()
        {
            base.Reset();

            Add(new[]
            {
                new Container()
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    AutoSizeAxes = Axes.Both,
                    Children = new[]
                    {
                        red = new DepthBox(Color4.Red, Anchor.TopLeft)
                        {
                            Position = new Vector2(-60),
                        },
                        blue = new DepthBox(Color4.Blue, Anchor.TopRight)
                        {
                            Position = new Vector2(60, -60),
                        },
                        green = new DepthBox(Color4.Green, Anchor.BottomRight)
                        {
                            Position = new Vector2(60),
                        },
                        purple = new DepthBox(Color4.Purple, Anchor.BottomLeft)
                        {
                            Position = new Vector2(-60, 60),
                        },
                    }
                }
            });

            AddStep($@"{nameof(red)} first", () =>
            {
                red.Depth = float.MinValue;
                blue.Depth = 0;
                green.Depth = 0;
                purple.Depth = 0;
            });
            AddStep($@"{nameof(blue)} first", () =>
            {
                red.Depth = 0;
                blue.Depth = float.MinValue;
                green.Depth = 0;
                purple.Depth = 0;
            });
            AddStep($@"{nameof(green)} first", () =>
            {
                red.Depth = 0;
                blue.Depth = 0;
                green.Depth = float.MinValue;
                purple.Depth = 0;
            });
            AddStep($@"{nameof(purple)} first", () =>
            {
                red.Depth = 0;
                blue.Depth = 0;
                green.Depth = 0;
                purple.Depth = float.MinValue;
            });
            AddStep(@"random depths", () =>
            {
                red.Depth = RNG.NextSingle();
                blue.Depth = RNG.NextSingle();
                green.Depth = RNG.NextSingle();
                purple.Depth = RNG.NextSingle();
            });
        }

        private class DepthBox : Container
        {
            private readonly SpriteText depthText;

            public DepthBox(Color4 colour, Anchor anchor)
            {
                Size = new Vector2(240);
                Anchor = Origin = Anchor.Centre;

                Add(new Drawable[]
                {
                    new Box()
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = colour,
                    },
                    depthText = new SpriteText()
                    {
                        Anchor = anchor,
                        Origin = anchor,
                    }
                });
            }

            protected override void Update()
            {
                base.Update();

                depthText.Text = $@"Depth: {Depth}";
            }
        }
    }
}
