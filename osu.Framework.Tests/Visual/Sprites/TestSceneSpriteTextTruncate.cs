// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Sprites
{
    public class TestSceneSpriteTextTruncate : FrameworkTestScene
    {
        private readonly FillFlowContainer flow;

        public TestSceneSpriteTextTruncate()
        {
            Children = new Drawable[]
            {
                new BasicScrollContainer
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
                new ExampleText(text, false, true, spacing: new Vector2(30)),
                new ExampleText(text, false, true, "…"),
                new ExampleText(text, false, true, "…", spacing: new Vector2(30)),
                new ExampleText(text, false, true, "--"),
                new ExampleText(text, false, true, "--", true),
                new ExampleText(text, false, true, "--", true, new Vector2(30)),
                new ExampleText(text, true, false),
                new ExampleText(text, true, true),
                new ExampleText(text, true, true, spacing: new Vector2(30)),
                new ExampleText(text, true, true, "…"),
                new ExampleText(text, true, true, "…", spacing: new Vector2(30)),
                new ExampleText(text, true, true, "--"),
                new ExampleText(text, true, true, "--", true),
                new ExampleText(text, true, true, "--", true, new Vector2(30)),
            });

            const float start_range = 10;
            const float end_range = 500;

            flow.Width = start_range;
            flow.ResizeWidthTo(end_range, 10000).Then().ResizeWidthTo(start_range, 10000).Loop();
        }

        private class ExampleText : Container
        {
            public ExampleText(string text, bool fixedWidth, bool truncate, string ellipsisString = "", bool runtimeChange = false, Vector2 spacing = new Vector2())
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
                    new CustomEllipsisSpriteText(ellipsisString, runtimeChange)
                    {
                        Text = text,
                        Truncate = truncate,
                        Spacing = spacing,
                        Font = new FontUsage(size: 20, fixedWidth: fixedWidth),
                        RelativeSizeAxes = Axes.X,
                        AllowMultiline = false
                    }
                };
            }
        }

        private class CustomEllipsisSpriteText : SpriteText
        {
            public CustomEllipsisSpriteText(string customEllipsis, bool runtimeChange)
            {
                EllipsisString = customEllipsis;

                if (runtimeChange)
                    Scheduler.AddDelayed(() => EllipsisString = customEllipsis == EllipsisString ? string.Empty : customEllipsis, 500, true);
            }
        }
    }
}
