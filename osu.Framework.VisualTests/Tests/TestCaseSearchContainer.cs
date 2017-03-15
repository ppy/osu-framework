// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Screens.Testing;
using System.Linq;
using System.Collections.Generic;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics;
using OpenTK;

namespace osu.Framework.VisualTests.Tests
{
    class TestCaseSearchContainer : TestCase
    {
        public override string Description => "Tests the SearchContainer";

        private SearchContainer search;

        public override void Reset()
        {
            base.Reset();
            List<SearchableText> searchable = new List<SearchableText>();
            searchable.Add(new SearchableText
            {
                Text = "test",
            });
            searchable.Add(new SearchableText
            {
                Text = "TEST",
            });
            searchable.Add(new SearchableText
            {
                Text = "123",
            });
            searchable.Add(new SearchableText
            {
                Text = "444",
            });
            searchable.Add(new SearchableText
            {
                Text = "öüäéèêáàâ",
            });
            Add(new TextBox
            {
                Size = new Vector2(300,40),
                OnChange = (textBox, newText) => search.SearchTerm = textBox.Text
            });
            Add(search = new SearchContainer
            {
                AutoSizeAxes = Axes.Both,
                Margin = new MarginPadding { Top = 40 },
                Children = new[]
                {
                    new HeaderContainer
                    {
                        AutoSizeAxes = Axes.Both,
                        Children = searchable,
                    }
                }
            });

        }

        private class HeaderContainer : Container, ISearchableChildren
        {
            public string[] Keywords => header.Keywords;
            public bool Matching
            {
                set
                {
                    if (value)
                        FadeIn();
                    else
                        FadeOut();
                }
            }
            public IEnumerable<ISearchable> SearchableChildren => Children.OfType<ISearchable>();

            protected override Container<Drawable> Content => flowContainer;

            private SearchableText header;
            private FillFlowContainer flowContainer;
            public HeaderContainer()
            {
                AddInternal(header = new SearchableText
                {   
                    Text = "Header",
                });
                AddInternal(flowContainer = new FillFlowContainer
                {
                    Margin = new MarginPadding { Top = header.TextSize, Left = 30 },
                    AutoSizeAxes = Axes.Both,
                    Direction = FillDirection.Vertical,
                });
            }
        }

        private class SearchableText : SpriteText, ISearchable
        {
            public string[] Keywords => new[] { Text };

            public bool Matching
            {
                set
                {
                    if (value)
                        FadeIn();
                    else
                        FadeOut();
                }
            }
        }
    }
}
