// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input;
using osu.Framework.Testing;
using OpenTK;
using OpenTK.Graphics;

namespace osu.Framework.Tests.Visual
{
    public class TestCaseNestedHover : TestCase
    {
        public TestCaseNestedHover()
        {
            HoverBox box1;
            Add(box1 = new HoverBox(Color4.Gray, Color4.White)
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Size = new Vector2(300, 300)
            });

            HoverBox box2;
            box1.Add(box2 = new HoverBox(Color4.Pink, Color4.Red)
            {
                RelativePositionAxes = Axes.Both,
                RelativeSizeAxes = Axes.Both,
                Position = new Vector2(0.2f, 0.2f),
                Size = new Vector2(0.6f, 0.6f)
            });

            box2.Add(new HoverBox(Color4.LightBlue, Color4.Blue, false)
            {
                RelativePositionAxes = Axes.Both,
                RelativeSizeAxes = Axes.Both,
                Position = new Vector2(0.2f, 0.2f),
                Size = new Vector2(0.6f, 0.6f)
            });
        }

        private class HoverBox : Container
        {
            private readonly Color4 normalColour;
            private readonly Color4 hoveredColour;

            private readonly Box box;
            private readonly bool propagateHover;

            public HoverBox(Color4 normalColour, Color4 hoveredColour, bool propagateHover = true)
            {
                this.normalColour = normalColour;
                this.hoveredColour = hoveredColour;
                this.propagateHover = propagateHover;

                Children = new Drawable[]
                {
                    box = new Box
                    {
                        Colour = normalColour,
                        RelativeSizeAxes = Axes.Both
                    }
                };
            }

            protected override bool OnHover(InputState state)
            {
                box.Colour = hoveredColour;
                return !propagateHover;
            }

            protected override void OnHoverLost(InputState state)
            {
                box.Colour = normalColour;
            }
        }
    }
}
