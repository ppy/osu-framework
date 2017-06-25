// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
using OpenTK.Graphics;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
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
                    Size = new Vector2(340),
                    Children = new[]
                    {
                        red = new DepthBox(Color4.Red, Anchor.TopLeft),
                        blue = new DepthBox(Color4.Blue, Anchor.TopRight),
                        green = new DepthBox(Color4.Green, Anchor.BottomRight),
                        purple = new DepthBox(Color4.Purple, Anchor.BottomLeft),
                    }
                }
            });

            AddStep($@"bring forward {nameof(red)}", () => red.Depth--);
            AddStep($@"bring forward {nameof(blue)}", () => blue.Depth--);
            AddStep($@"bring forward {nameof(green)}", () => green.Depth--);
            AddStep($@"bring forward {nameof(purple)}", () => purple.Depth--);

            AddStep($@"send backward {nameof(red)}", () => red.Depth++);
            AddStep($@"send backward {nameof(blue)}", () => blue.Depth++);
            AddStep($@"send backward {nameof(green)}", () => green.Depth++);
            AddStep($@"send backward {nameof(purple)}", () => purple.Depth++);
        }

        private class DepthBox : Container
        {
            private readonly SpriteText depthText;

            public DepthBox(Color4 colour, Anchor anchor)
            {
                Size = new Vector2(240);
                Anchor = Origin = anchor;

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
