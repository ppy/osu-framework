// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Testing;
using OpenTK;
using OpenTK.Graphics;

namespace osu.Framework.Tests.Visual
{
    [System.ComponentModel.Description("changing depth of child dynamically")]
    public class TestCaseDynamicDepth : TestCase
    {
        private void addDepthSteps(DepthBox box, Container container)
        {
            AddStep($@"bring forward {box.Name}", () => container.ChangeChildDepth(box, box.Depth - 1));
            AddStep($@"send backward {box.Name}", () => container.ChangeChildDepth(box, box.Depth + 1));
        }

        public TestCaseDynamicDepth()
        {
            DepthBox red, blue, green, purple;
            Container container;

            AddRange(new[]
            {
                container = new Container
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Size = new Vector2(340),
                    Children = new[]
                    {
                        red = new DepthBox(Color4.Red, Anchor.TopLeft) { Name = "red" },
                        blue = new DepthBox(Color4.Blue, Anchor.TopRight) { Name = "blue" },
                        green = new DepthBox(Color4.Green, Anchor.BottomRight) { Name = "green" },
                        purple = new DepthBox(Color4.Purple, Anchor.BottomLeft) { Name = "purple" },
                    }
                }
            });

            addDepthSteps(red, container);
            addDepthSteps(blue, container);
            addDepthSteps(green, container);
            addDepthSteps(purple, container);
        }

        private class DepthBox : Container
        {
            private readonly SpriteText depthText;

            public DepthBox(Color4 colour, Anchor anchor)
            {
                Size = new Vector2(240);
                Anchor = Origin = anchor;

                AddRange(new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = colour,
                    },
                    depthText = new SpriteText
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
