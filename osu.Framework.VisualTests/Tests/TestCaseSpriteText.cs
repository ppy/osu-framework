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

            Container paragraphContainer;
            Paragraph paragraph;
            flow.Add(paragraphContainer = new Container
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
                    paragraph = new Paragraph
                    {
                        HeaderIndent = 5,
                        BodyIndent = 10,
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

            paragraph.AddText(@"the considerably swift vermilion reynard bounds above the slothful mahogany hound.", t => t.Colour = Color4.Yellow);
            paragraph.AddText("\n\n\n\n\n\nTHE CONSIDERABLY SWIFT VERMILION REYNARD BOUNDS ABOVE THE SLOTHFUL MAHOGANY HOUND!!", t => t.Colour = Color4.Red);
            paragraph.AddText("\n\n0123456789!@#$%^&*()_-+-[]{}.,<>;'\\\\", t => t.Colour = Color4.Blue);
            paragraph.AddText("\n\nI'm a paragraph, newlines are cool", t => t.Colour = Color4.Beige);

            AddStep(@"resize paragraph 1", () => { paragraphContainer.Width = 200f; });
            AddStep(@"resize paragraph 2", () => { paragraphContainer.Width = 500f; });
            AddStep(@"header inset", () => { paragraph.HeaderIndent += 2; });
            AddStep(@"body inset", () => { paragraph.BodyIndent += 4; });
        }
    }
}
