// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Testing;
using OpenTK;

namespace osu.Framework.Tests.Visual
{
    public class TestCaseSearchContainer : TestCase
    {
        public TestCaseSearchContainer()
        {
            SearchContainer<HeaderContainer> search;
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
                                    Children = new Drawable[]
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
                                        new FilterableFlowContainer
                                        {
                                            Direction = FillDirection.Horizontal,
                                            AutoSizeAxes = Axes.Both,
                                            Children = new []
                                            {
                                                new SpriteText
                                                {
                                                    Text = "multi",
                                                },
                                                new SpriteText
                                                {
                                                    Text = "piece",
                                                },
                                                new SpriteText
                                                {
                                                    Text = "container",
                                                },
                                            }
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
                { "sUbSeCtIoN 1", 6 },
                { "€", 1 },
                { "èê", 1 },
                { "321", 0 },
                { "mul pi", 1},
                { "header", 8 }
            }.ToList().ForEach(term =>
            {
                AddStep("Search term: " + term.Key, () => search.SearchTerm = term.Key);
                AddAssert("Visible end-children: " + term.Value, () => term.Value == search.Children.SelectMany(container => container.Children.Cast<Container>()).SelectMany(container => container.Children).Count(drawable => drawable.IsPresent));
            });

            textBox.Current.ValueChanged += newValue => search.SearchTerm = newValue;
        }

        private class HeaderContainer : Container, IHasFilterableChildren
        {
            public IEnumerable<string> FilterTerms => header.FilterTerms;

            public bool MatchingFilter
            {
                set
                {
                    if (value)
                        this.FadeIn();
                    else
                        this.FadeOut();
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

        private class FilterableFlowContainer : FillFlowContainer, IFilterable
        {
            public IEnumerable<string> FilterTerms => Children.OfType<IHasFilterTerms>().SelectMany(d => d.FilterTerms);

            public bool MatchingFilter
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

        private class SearchableText : SpriteText, IFilterable
        {
            public bool MatchingFilter
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
