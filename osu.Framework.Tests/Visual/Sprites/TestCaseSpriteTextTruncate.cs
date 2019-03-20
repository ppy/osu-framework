// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Testing;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Sprites
{
    public class TestCaseSpriteTextTruncate : TestCase
    {
        public TestCaseSpriteTextTruncate()
        {
            FillFlowContainer flow;

            Children = new Drawable[]
            {
                new ScrollContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Children = new[]
                    {
                        flow = new FillFlowContainer
                        {
                            Anchor = Anchor.TopLeft,
                            AutoSizeAxes = Axes.Y,
                            RelativeSizeAxes = Axes.X,
                            Direction = FillDirection.Vertical,
                        }
                    }
                }
            };
            AddStep(@"Variable width", () => { addText(flow); });
            AddStep(@"Fixed width", () => { addText(flow, fixedWidth: true); });
        }

        private class ExampleText : Container
        {
            public ExampleText(string text, float width, bool fixedWidth, bool truncate, string ellipsisString = "")
            {
                AutoSizeAxes = Axes.Y;
                Width = width;
                Children = new Drawable[]
                {
                    new Box
                    {
                        Colour = Color4.DarkMagenta,
                        RelativeSizeAxes = Axes.Both,
                    },
                    new CustomEllipsisSpriteText(ellipsisString)
                    {
                        Text = text,
                        Truncate = truncate,
                        Font = new FontUsage(size: 20, fixedWidth: fixedWidth),
                        RelativeSizeAxes = Axes.X,
                        AllowMultiline = false
                    }
                };
            }
        }

        private static void addText(FillFlowContainer flow, string text = "A really really really really long text passage", bool fixedWidth = false, int startWidth = 20, int endWidth = 270, int step = 10)
        {
            flow.Clear();

            for (int width = startWidth; width < endWidth; width += step)
            {
                flow.AddRange(new Drawable[]
                {
                    new SpriteText { Text = $"width = {width}" },
                    new ExampleText(text, width, fixedWidth, false),
                    new ExampleText(text, width, fixedWidth, true),
                    new ExampleText(text, width, fixedWidth, true, "…"),
                    new ExampleText(text, width, fixedWidth, true, "--"),
                });
            }
        }

        private class CustomEllipsisSpriteText : SpriteText
        {
            public CustomEllipsisSpriteText(string s)
            {
                EllipsisString = s;
            }
        }
    }
}
