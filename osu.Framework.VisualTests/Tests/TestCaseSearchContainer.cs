// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Testing;
using System.Linq;
using System.Collections.Generic;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Graphics;
using OpenTK;

namespace osu.Framework.VisualTests.Tests
{
    internal class TestCaseSearchContainer : TestCase
    {
        public override string Description => "Tests the SearchContainer";

        private SearchContainer<HeaderContainer> search;

        public override void Reset()
        {
            base.Reset();
            TextBox textBox;

            Children = new Drawable[] {
                textBox = new TextBox
                {
                    Size = new Vector2(300, 40),
                },
                search = new SearchContainer<HeaderContainer>
                {
                    AutoSizeAxes = Axes.Both,
                    Margin = new MarginPadding { Top = 40 },
                    Children = new[]
                    {
                        new HeaderContainer
                        {
                            AutoSizeAxes = Axes.Both,
                            Children = new[]
                            {
                                new HeaderContainer("Subsection 1")
                                {
                                    AutoSizeAxes = Axes.Both,
                                    Children = new[]
                                    {
                                        new SearchableText
                                        {
                                            Text = "test",
                                        },
                                        new SearchableText
                                        {
                                            Text = "TEST",
                                        },
                                        new SearchableText
                                        {
                                            Text = "123",
                                        },
                                        new SearchableText
                                        {
                                            Text = "444",
                                        },
                                        new SearchableText
                                        {
                                            Text = "öüäéèêáàâ",
                                        }
                                    }
                                },
                                new HeaderContainer("Subsection 2")
                                {
                                    AutoSizeAxes = Axes.Both,
                                    Children = new[]
                                    {
                                        new SearchableText
                                        {
                                            Text = "?!()[]{}"
                                        },
                                        new SearchableText
                                        {
                                            Text = "@€$"
                                        },
                                    },
                                },
                            },
                        }
                    }
                }
            };

            new Dictionary<string, int>
            {
                { "test", 2 },
                { "sUbSeCtIoN 1", 5 },
                { "€", 1 },
                { "èê", 1 },
                { "321", 0 },
                { "header", 7 }
            }.ToList().ForEach(term => {
                AddStep("Search term: " + term.Key, () => search.SearchTerm = term.Key);
                AddAssert("Visible end-children: " + term.Value, () => term.Value == search.Children.SelectMany(container => container.Children.Cast<Container>()).SelectMany(container => container.Children).Count(drawable => drawable.IsPresent));
            });

            textBox.Current.ValueChanged += newValue => search.SearchTerm = newValue;
        }

        private class HeaderContainer : Container, IHasFilterableChildren
        {
            public string[] FilterTerms => header.FilterTerms;
            public bool MatchingCurrentFilter
            {
                set
                {
                    if (value)
                        FadeIn();
                    else
                        FadeOut();
                }
            }
            public IEnumerable<IFilterable> FilterableChildren => Children.OfType<IFilterable>();

            protected override Container<Drawable> Content => flowContainer;

            private readonly SearchableText header;
            private readonly FillFlowContainer flowContainer;

            public HeaderContainer(string headerText = "Header")
            {
                AddInternal(header = new SearchableText
                {
                    Text = headerText,
                });
                AddInternal(flowContainer = new FillFlowContainer
                {
                    Margin = new MarginPadding { Top = header.TextSize, Left = 30 },
                    AutoSizeAxes = Axes.Both,
                    Direction = FillDirection.Vertical,
                });
            }
        }

        private class SearchableText : SpriteText, IFilterable
        {
            public string[] FilterTerms => new[] { Text };

            public bool MatchingCurrentFilter
            {
                set
                {
                    if (value)
                        Show();
                    else
                        Hide();
                }
            }
        }
    }
}
