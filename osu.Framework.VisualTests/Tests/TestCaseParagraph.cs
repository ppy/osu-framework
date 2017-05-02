// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK.Graphics;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Testing;

namespace osu.Framework.VisualTests.Tests
{
    internal class TestCaseParagraph : TestCase
    {
        public override string Description => @"Test word-wrapping and paragraphs";

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
                            AutoSizeAxes = Axes.Y,
                            RelativeSizeAxes = Axes.X,
                            Direction = FillDirection.Vertical,
                        }
                    }
                }
            };

            FillFlowContainer paragraphContainer;
            Paragraph paragraph;
            flow.Add(paragraphContainer = new FillFlowContainer
            {
                AutoSizeAxes = Axes.Y,
                RelativeSizeAxes = Axes.X,
                Width = 0.5f,
                Direction = FillDirection.Vertical,
                Children = new Drawable[]
                {
                    paragraph = new Paragraph
                    {
                        FirstLineIndent = 5,
                        ContentIndent = 10,
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                    }
                }
            });

            paragraph.AddText("the considerably swift vermilion reynard bounds above the slothful mahogany hound.", t => t.Colour = Color4.Yellow);
            paragraph.AddText("\nTHE ", t => t.Colour = Color4.Red);
            paragraph.AddText("CONSIDERABLY", t => t.Colour = Color4.Pink);
            paragraph.AddText(" SWIFT VERMILION REYNARD BOUNDS ABOVE THE SLOTHFUL MAHOGANY HOUND!!", t => t.Colour = Color4.Red);
            paragraph.AddText("\n\n0123456789!@#$%^&*()_-+-[]{}.,<>;'\\\\", t => t.Colour = Color4.Blue);
            paragraph.AddText("\nI'm a paragraph\nnewlines are cool", t => t.Colour = Color4.Beige);
            paragraph.AddText(" (and so are inline styles!)", t => t.Colour = Color4.Yellow);

            paragraphContainer.Add(new Paragraph
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Text =
@"osu! is a freeware rhythm game developed by Dean ""peppy"" Herbert, originally for Microsoft Windows. The game has also been ported to macOS, iOS, Android, and Windows Phone.[1] Its game play is based on commercial titles including Osu! Tatakae! Ouendan, Elite Beat Agents, Taiko no Tatsujin, beatmania IIDX, O2Jam, and DJMax.

osu! is written in C# on the .NET Framework. On August 28, 2016, osu!'s source code was open-sourced under the MIT License. [2] [3] Dubbed as ""Lazer"", the project aims to make osu! available to more platforms and transparent. [4] The community includes over 9 million registered users, with a total of 6 billion ranked plays.[5]"
            });

            AddStep(@"resize paragraph 1", () => { paragraphContainer.Width = 1f; });
            AddStep(@"resize paragraph 2", () => { paragraphContainer.Width = 0.6f; });
            AddStep(@"header inset", () => { paragraph.FirstLineIndent += 2; });
            AddStep(@"body inset", () => { paragraph.ContentIndent += 4; });
        }
    }
}
