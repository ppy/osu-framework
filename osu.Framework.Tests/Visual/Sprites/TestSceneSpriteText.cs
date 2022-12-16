// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Sprites
{
    public partial class TestSceneSpriteText : FrameworkTestScene
    {
        public TestSceneSpriteText()
        {
            FillFlowContainer flow;

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
                            RelativeSizeAxes = Axes.X,
                            Direction = FillDirection.Vertical,
                        }
                    }
                }
            };

            flow.Add(new SpriteText
            {
                Text = @"the quick red fox jumps over the lazy brown dog"
            });
            flow.Add(new SpriteText
            {
                Text = @"THE QUICK RED FOX JUMPS OVER THE LAZY BROWN DOG"
            });
            flow.Add(new SpriteText
            {
                Text = @"0123456789!@#$%^&*()_-+-[]{}.,<>;'\"
            });

            flow.Add(new Container
            {
                Margin = new MarginPadding { Vertical = 5 },
                AutoSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                    },
                    new SpriteText
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Colour = Color4.Black,
                        UseFullGlyphHeight = true,
                        Text = "UseFullGlyphHeight = true",
                    },
                }
            });

            flow.Add(new Container
            {
                Margin = new MarginPadding { Vertical = 5 },
                AutoSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                    },
                    new SpriteText
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Colour = Color4.Black,
                        UseFullGlyphHeight = false,
                        Text = "UseFullGlyphHeight = false",
                    },
                }
            });

            for (int i = 1; i <= 200; i++)
            {
                SpriteText text = new SpriteText
                {
                    Text = $@"Font testy at size {i}",
                    Font = new FontUsage("Roboto", i, i % 4 > 1 ? "Bold" : "Regular", i % 2 == 1),
                    AllowMultiline = true,
                    RelativeSizeAxes = Axes.X,
                };

                flow.Add(text);
            }
        }
    }
}
