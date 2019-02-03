// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Testing;

namespace osu.Framework.Tests.Visual
{
    public class TestCaseSpriteText : TestCase
    {
        public TestCaseSpriteText()
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

            for (int i = 1; i <= 200; i++)
            {
                string font = "OpenSans-";
                if (i % 4 > 1)
                    font += "Bold";
                if (i % 2 == 1)
                    font += "Italic";

                font = font.TrimEnd('-');

                SpriteText text = new SpriteText
                {
                    Text = $@"Font testy at size {i}",
                    Font = font,
                    AllowMultiline = true,
                    RelativeSizeAxes = Axes.X,
                    TextSize = i
                };

                flow.Add(text);
            }
        }
    }
}
