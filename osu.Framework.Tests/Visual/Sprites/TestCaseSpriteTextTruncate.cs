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
        private readonly FillFlowContainer flow;

        public TestCaseSpriteTextTruncate()
        {
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
                            Direction = FillDirection.Vertical,
                        }
                    }
                }
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            const string text = "A really really really really long text passage";

            flow.AddRange(new Drawable[]
            {
                new ExampleText(text, false, false),
                new ExampleText(text, false, true),
                new ExampleText(text, false, true, "…"),
                new ExampleText(text, false, true, "--"),
                new ExampleText(text, true, false),
                new ExampleText(text, true, true),
                new ExampleText(text, true, true, "…"),
                new ExampleText(text, true, true, "--"),
            });

            const float start_range = 10;
            const float end_range = 500;

            flow.Width = start_range;
            flow.ResizeWidthTo(end_range, 10000).Then().ResizeWidthTo(start_range, 10000).Loop();
        }

        private class ExampleText : Container
        {
            public ExampleText(string text, bool fixedWidth, bool truncate, string ellipsisString = "")
            {
                AutoSizeAxes = Axes.Y;
                RelativeSizeAxes = Axes.X;
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

        private class CustomEllipsisSpriteText : SpriteText
        {
            public CustomEllipsisSpriteText(string s)
            {
                EllipsisString = s;
            }
        }
    }
}
