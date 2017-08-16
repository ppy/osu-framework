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
            TextBox textBox;
            MarkdownContainer textBoxMarkdown;
            Child = new FillFlowContainer
            {
                Direction = FillDirection.Vertical,
                RelativeSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    new MarkdownContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        FormattedText = "a _little_ *Test*\n" +
                                        "*a marker without an end"
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
