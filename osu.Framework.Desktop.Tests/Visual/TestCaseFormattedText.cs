// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Testing;
using OpenTK.Graphics;
using osu.Framework.Graphics;
using osu.Framework.Graphics.UserInterface;
using OpenTK;
using System.Collections.Generic;

namespace osu.Framework.Desktop.Tests.Visual
{
    internal class TestCaseFormattedText : TestCase
    {
        public override string Description => "tests formatting text";

        public TestCaseFormattedText()
        {
            const string text = "a _little_ *test*\n" +
                          "_multiple *markers* at once_\n" +
                          "*incorrectly _ordered* markers_\n" +
                          "*markers across\n" +
                          "multiple lines*\n" +
                          "*a marker without an end";

            TextBox textBox;
            MarkdownContainer textBoxMarkdown;
            Children = new Drawable[]
            {
                new FillFlowContainer
                {
                    Direction = FillDirection.Vertical,
                    RelativeSizeAxes = Axes.Both,
                    Children = new Drawable[]
                    {
                        new MarkdownContainer
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            FormattedText = text,
                        },
                        new MarkdownContainerEndless
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            FormattedText = "*a marker without an end but the parser doesn't care",
                        },
                        textBox = new TextBox
                        {
                            Size = new Vector2(300, 30),
                        },
                        textBoxMarkdown = new MarkdownContainer
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                        }
                    }
                },
                new TextFlowContainer
                {
                    Anchor = Anchor.TopRight,
                    Origin = Anchor.TopRight,
                    RelativeSizeAxes = Axes.Both,
                    Width = 0.5f,
                    Text = text,
                },
            };

            textBox.Current.ValueChanged += newValue => textBoxMarkdown.FormattedText = newValue;
            textBox.Current.Value = "_test_ *here*";
        }

        private class MarkdownContainer : FormattedTextFlowContainer<Markers>
        {
            protected override Dictionary<string, Markers> MarkerDelimeters => new Dictionary<string, Markers>
            {
                { "_",  Markers.Small },
                { "*", Markers.Red },
            };

            protected override void FormatText(List<Markers> markers, SpriteText text)
            {
                if (markers.Contains(Markers.Red))
                    text.Colour = Color4.Red;

                if (markers.Contains(Markers.Small))
                    text.TextSize = 15;
            }
        }

        private class MarkdownContainerEndless : MarkdownContainer
        {
            protected override bool MarkerNeedsEnd => false;
        }

        private enum Markers
        {
            None,
            Small,
            Red,
        }
    }
}
