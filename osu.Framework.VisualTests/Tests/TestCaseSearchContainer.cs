// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.MathUtils;
using osu.Framework.Screens.Testing;
using System;
using System.Linq;
using System.Collections.Generic;
using OpenTK.Graphics;

namespace osu.Framework.VisualTests.Tests
{
    internal class TestCaseSearchContainer : TestCase
    {
        public override string Description => "Tests the SearchContainer";

        private SearchContainer search;
        private TextBox textBox;
        private List<SearchableText> text;

        public override void Reset()
        {
            base.Reset();

            text = new List<SearchableText>();
            for(int i = 0; i < 8; i++)
            {
                text.Add(new SearchableText
                {
                    Text = RNG.Next(1000).ToString(),
                });
            }
            text.Add(new SearchableText
            {
                Text = "TEST",
            });
            text.Add(new SearchableText
            {
                Text = "test",
            });

            Children = new Drawable[]
            {
                textBox = new TextBox
                {
                    Size = new Vector2(300,30),
                    OnChange = delegate
                    {
                        search.Filter = textBox.Text;
                    },
                },
                search = new SearchContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Children = new Drawable[]
                    {
                        new SearchableFlowContainer<SearchableText>()
                        {
                            Direction = FillDirection.Vertical,
                            Children = text,
                            RelativeSizeAxes = Axes.Both,
                            Margin = new MarginPadding { Top = 30 },
                        }
                    },
                    OnMatch = match,
                    OnMismatch = mismatch,
                    AfterSearch = afterSearch,
                }
            };
        }

        private void match(Drawable searchable) => searchable.FadeIn(100);

        private void mismatch(Drawable searchable) => searchable.FadeOut(100);

        private void afterSearch() => Scheduler.AddDelayed(() => text.ForEach((SearchableText textColorChange) => textColorChange.FadeColour(text.All((SearchableText text) => text.IsPresent) ? Color4.White : Color4.Red, 100)),105);
        

        private class SearchableText : SpriteText, ISearchable
        {
            public string[] Keywords => new [] { Text };
            public bool LastMatch { get; set; }
        }

        private class SearchableFlowContainer<T> : FillFlowContainer<T>, ISearchableChildren where T : Drawable
        {
            public string[] Keywords => new[] { "flowcontainer" };
            public bool LastMatch { get; set; }
            public IEnumerable<Drawable> SearchableChildren => Children;

            public Action AfterSearch => null;
        }
    }

    
}
