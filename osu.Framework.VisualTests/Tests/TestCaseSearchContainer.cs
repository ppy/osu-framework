using osu.Framework.Graphics;
using osu.Framework.Screens.Testing;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.UserInterface;
using OpenTK;
using osu.Framework.Graphics.Sprites;
using osu.Framework.MathUtils;
using osu.Framework.Graphics.Primitives;
using System;
using System.Collections.Generic;

namespace osu.Framework.VisualTests.Tests
{
    class TestCaseSearchContainer : TestCase
    {
        public override string Description => "Tests the SearchContainer";

        private SearchContainer search;
        private TextBox textBox;

        public override void Reset()
        {
            base.Reset();

            SearchableText[] text = new SearchableText[10];
            for(int i = 0; i < text.Length-2; i++)
            {
                text[i] = new SearchableText
                {
                    Position = new Vector2(0,30*i+30),
                    Text = RNG.Next(1000).ToString(),
                };
            }
            text[8] = new SearchableText
            {
                
                Text = "TEST",
            };
            text[9] = new SearchableText
            {

                Text = "test",
            };

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
                }
            };
        }

        private void match(Drawable searchable)
        {
            searchable.FadeIn(200);
        }

        private void mismatch(Drawable searchable)
        {
            searchable.FadeOut(200);
        }

        private class SearchableText : SpriteText, ISearchable
        {
            public string[] Keywords => new string[] { Text };
        }

        private class SearchableFlowContainer<T> : FillFlowContainer<T>, ISearchableChildren where T : Drawable
        {
            public IEnumerable<Drawable> SearchableChildren => Children;

            public Action AfterSearch => null;
        }
    }

    
}
