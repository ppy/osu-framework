// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK.Graphics;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Testing;

namespace osu.Framework.VisualTests.Tests
{
    internal class TestCaseSpriteText : TestCase
    {
        public override string Description => @"Test all sizes of text rendering";

        public override void Reset()
        {
            base.Reset();

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
                            AutoSizeAxes = Axes.Both,
                            Direction = FillDirection.Vertical,
                        }
                    }
                }
            };

            flow.Add(new Container
            {
                Width = 350,
                AutoSizeAxes = Axes.Y,
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.White.Opacity(0.25f),
                    },
                    new Paragraph
                    {
                        Text = "the considerably swift vermilion reynard bounds above the slothful mahogany hound.\n\n\n\n\n\nTHE CONSIDERABLY SWIFT VERMILION REYNARD BOUNDS ABOVE THE SLOTHFUL MAHOGANY HOUND!!\n\n0123456789!@#$%^&*()_-+-[]{}.,<>;'\\\n\nI'm a paragraph, newlines are cool",
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                    }
                }
            });
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
                SpriteText text = new SpriteText
                {
                    Text = $@"Font testy at size {i}",
                    TextSize = i
                };

                flow.Add(text);
            }
        }
    }
}
