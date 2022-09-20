// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using BenchmarkDotNet.Attributes;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;

namespace osu.Framework.Benchmarks
{
    public class BenchmarkSearchContainer : GameBenchmark
    {
        [Params(true, false)]
        public bool AllowNonContiguous { get; set; }

        [Params(true, false)]
        public bool IgnoreNonSpace { get; set; }

        private TestGame game = null!;

        [Benchmark]
        public void SearchRandomStrings()
        {
            game.Search.AllowNonContiguousMatching = AllowNonContiguous;
            game.Search.IgnoreNonSpace = IgnoreNonSpace;
            foreach (string searchTerm in game.SearchTerms)
            {
                game.Search.SearchTerm = searchTerm;
                RunSingleFrame();
            }
        }

        protected override Game CreateGame() => game = new TestGame();

        private class TestGame : Game
        {
            public SearchContainer Search = null!;
            public string[] SearchTerms = null!;
            private const string chars = "ABCDEFGHIJKL MNOPQRSTUVWXYZ 0123456789 ĄĆĘŁŃÓŚŹŻ abcdefghijkl mnopqrstuvwxyz ąćęłńóśźż ";
            private static Random random = new Random();

            protected override void LoadComplete()
            {
                base.LoadComplete();

                var searchableTexts = Enumerable.Range(0, 1000)
                                                .Select(_ => new SearchableText { Text = RandomString(50) })
                                                .ToArray();

                SearchTerms = Enumerable.Range(0, 1000)
                                        .Select(_ => RandomString(10))
                                        .ToArray();

                Add(Search = new SearchContainer
                {
                    Children = searchableTexts,
                });
            }

            private static string RandomString(int length) => new string(Enumerable.Repeat(chars, length)
                                                                        .Select(s => s[random.Next(s.Length)])
                                                                        .ToArray());

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
}
