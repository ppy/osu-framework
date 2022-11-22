// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Localisation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Tests.Visual.Localisation;
using osuTK;

namespace osu.Framework.Tests.Visual.UserInterface
{
    public class TestSceneSearchContainer : LocalisationTestScene
    {
        private SearchContainer search;
        private BasicTextBox textBox;

        [BackgroundDependencyLoader]
        private void load()
        {
            Manager.AddLanguage("en", new TestLocalisationStore("en", new Dictionary<string, string>
            {
                [goodbye] = "Goodbye",
            }));
            Manager.AddLanguage("es", new TestLocalisationStore("es", new Dictionary<string, string>
            {
                [goodbye] = "Adiós",
            }));
        }

        private const string goodbye = "goodbye";

        [SetUp]
        public void SetUp() => Schedule(() =>
        {
            Children = new Drawable[]
            {
                textBox = new BasicTextBox
                {
                    Size = new Vector2(300, 40),
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
                            Children = new[]
                            {
                                new HeaderContainer("Subsection 1")
                                {
                                    AutoSizeAxes = Axes.Both,
                                    Children = new Drawable[]
                                    {
                                        new SearchableText { Text = "test", },
                                        new SearchableText { Text = "TEST", },
                                        new SearchableText { Text = "123", },
                                        new SearchableText { Text = "444", },
                                        new FilterableFlowContainer
                                        {
                                            Direction = FillDirection.Horizontal,
                                            AutoSizeAxes = Axes.Both,
                                            Children = new[]
                                            {
                                                new SpriteText { Text = "multi", },
                                                new SpriteText { Text = "piece", },
                                                new SpriteText { Text = "container", },
                                            }
                                        },
                                        new SearchableText { Text = "öüäéèêáàâ", }
                                    }
                                },
                                new HeaderContainer("Subsection 2")
                                {
                                    AutoSizeAxes = Axes.Both,
                                    Children = new[]
                                    {
                                        new SearchableText { Text = "?!()[]{}" },
                                        new SearchableText { Text = "@€$" },
                                        new SearchableText { Text = new LocalisableString(new TranslatableString(goodbye, "Goodbye")) },
                                    },
                                },
                            },
                        }
                    }
                }
            };

            textBox.Current.ValueChanged += e => search.SearchTerm = e.NewValue;
        });

        [TestCase("test", 2)]
        [TestCase("sUbSeCtIoN 1", 6)]
        [TestCase("€", 1)]
        [TestCase("èê", 1)]
        [TestCase("321", 0)]
        [TestCase("mul pi", 1)]
        [TestCase("header", 9)]
        public void TestFiltering(string term, int count)
        {
            setTerm(term);
            checkCount(count);
        }

        [TestCase("tst", 2)]
        [TestCase("ssn 1", 6)]
        [TestCase("sns 1", 0)]
        [TestCase("hdr", 9)]
        [TestCase("tt", 2)]
        [TestCase("ttt", 0)]
        public void TestEagerFilteringEnabled(string term, int count)
        {
            AddStep("set non-contiguous on", () => search.AllowNonContiguousMatching = true);
            setTerm(term);
            checkCount(count);
        }

        [TestCase("tst", 0)]
        [TestCase("ssn 1", 0)]
        [TestCase("sns 1", 0)]
        [TestCase("hdr", 0)]
        [TestCase("tt", 0)]
        [TestCase("ttt", 0)]
        public void TestEagerFilteringDisabled(string term, int count)
        {
            AddStep("set non-contiguous off", () => search.AllowNonContiguousMatching = false);
            setTerm(term);
            checkCount(count);
        }

        [TestCase]
        public void TestRefilterAfterNewChild()
        {
            setTerm("multi");
            checkCount(1);
            AddStep("Add new filtered item", () => search.Add(new SearchableText { Text = "not visible" }));
            checkCount(1);
            AddStep("Add new unfiltered item", () => search.Add(new SearchableText { Text = "multi visible" }));
            checkCount(2);
        }

        [TestCase]
        public void TestFilterLocalisedStrings()
        {
            SetLocale("en");
            setTerm("Goodbye");
            checkCount(1);
            SetLocale("es");
            setTerm("Adiós");
            checkCount(1);
            setTerm("Goodbye");
            checkCount(1);
        }

        private void checkCount(int count)
        {
            AddAssert("Visible children: " + count, () => count == countSearchableText(search));
        }

        private int countSearchableText(CompositeDrawable container)
        {
            return container.InternalChildren.Where(t => t is SearchableText || t is FilterableFlowContainer).Count(c => c.IsPresent)
                   + container.InternalChildren.Where(c => c.IsPresent).OfType<CompositeDrawable>().Sum(countSearchableText);
        }

        private void setTerm(string term)
        {
            AddStep("Search term: " + term, () => textBox.Text = term);
        }

        private class HeaderContainer : Container, IHasFilterableChildren
        {
            public IEnumerable<LocalisableString> FilterTerms => header.FilterTerms;

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

            public bool FilteringActive
            {
                set { }
            }

            public IEnumerable<IFilterable> FilterableChildren => Children.OfType<IFilterable>();

            protected override Container<Drawable> Content => flowContainer;

            private readonly HeaderText header;
            private readonly FillFlowContainer flowContainer;

            public HeaderContainer(string headerText = "Header")
            {
                AddInternal(header = new HeaderText
                {
                    Text = headerText,
                });
                AddInternal(flowContainer = new FillFlowContainer
                {
                    Margin = new MarginPadding { Top = header.Font.Size, Left = 30 },
                    AutoSizeAxes = Axes.Both,
                    Direction = FillDirection.Vertical,
                });
            }
        }

        private class FilterableFlowContainer : FillFlowContainer, IFilterable
        {
            public IEnumerable<LocalisableString> FilterTerms => Children.OfType<IHasFilterTerms>().SelectMany(d => d.FilterTerms);

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

            public bool FilteringActive
            {
                set { }
            }
        }

        private class HeaderText : SpriteText, IFilterable
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

            public bool FilteringActive
            {
                set { }
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

            public bool FilteringActive
            {
                set { }
            }
        }
    }
}
