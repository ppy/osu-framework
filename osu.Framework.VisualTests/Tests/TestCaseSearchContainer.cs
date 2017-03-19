// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

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
    internal class TestCaseSearchContainer : TestCase
    {
        public override string Description => "Tests the SearchContainer";

        private SearchContainer search;

        public override void Reset()
        {
            base.Reset();
            Children = new Drawable[] {
                new TextBox
                {
                    Size = new Vector2(300, 40),
                    OnChange = (textBox, newText) => search.SearchTerm = textBox.Text
                },
                search = new SearchContainer
                {
                    AutoSizeAxes = Axes.Both,
                    Margin = new MarginPadding { Top = 40 },
                    Children = new[]
                    {
                        new HeaderContainer
                        {
                            AutoSizeAxes = Axes.Both,
                            Children = new []
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
                                    Children = new []
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

        }

        private class HeaderContainer : Container, IFilterableChildren
        {
            public string[] Keywords => header.Keywords;
            public bool FilteredByParent
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

            private SearchableText header;
            private FillFlowContainer flowContainer;

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
            public string[] Keywords => new[] { Text };

            public bool FilteredByParent
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
