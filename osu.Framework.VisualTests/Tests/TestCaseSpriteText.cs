using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;

namespace osu.Framework.VisualTests.Tests
{
    class TestCaseSpriteText : TestCase
    {
        internal override string Name => @"SpriteText";

        internal override string Description => @"Test all sizes of text rendering";

        internal override void Reset()
        {
            base.Reset();

            FlowContainer flow;

            Children = new Drawable[]
            {
                new ScrollContainer()
                {
                    Children = new [] {
                        flow = new FlowContainer()
                        {
                            Anchor = Anchor.TopLeft,
                            Direction = FlowDirection.VerticalOnly,
                        }
                    }
                }
            };

            for (int i = 1; i <= 200; i++)
            {
                SpriteText text = new SpriteText()
                {
                    Text = $@"Font testy at size {i}",
                    TextSize = i
                };

                flow.Add(text);
            }
        }
    }
}
