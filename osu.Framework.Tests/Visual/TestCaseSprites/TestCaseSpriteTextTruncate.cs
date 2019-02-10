// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Testing;

namespace osu.Framework.Tests.Visual.TestCaseSprites
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

        private static void addText(FillFlowContainer flow, string text = "A really really really really long textbox", bool fixedWidth = false, int startWidth = 20, int endWidth = 270, int step = 10)
        {
            flow.Clear();
            for (int a = startWidth; a < endWidth; a += step)
            {
                flow.Add(new SpriteText
                {
                    Text = text,
                    Truncate = true,
                    TextSize = 20,
                    FixedWidth = fixedWidth,
                    Width = a,
                    AllowMultiline = false
                });
                flow.Add(new SpriteText
                {
                    Text = text,
                    TruncateWithEllipsis = true,
                    TextSize = 20,
                    FixedWidth = fixedWidth,
                    Width = a,
                    AllowMultiline = false
                });
            }
        }
    }
}
