// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Linq;
using NUnit.Framework;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Testing;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Containers
{
    public partial class TestSceneTextFlowContainer : FrameworkTestScene
    {
        private const string default_text = "Default text\n\nnewline";

        private TextFlowContainer textContainer;

        [SetUp]
        public void Setup() => Schedule(() =>
        {
            Child = new Container
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Width = 300,
                AutoSizeAxes = Axes.Y,
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.White.Opacity(0.1f)
                    },
                    textContainer = new TextFlowContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Text = default_text
                    }
                }
            };
        });

        [TestCase(Anchor.TopLeft)]
        [TestCase(Anchor.TopCentre)]
        [TestCase(Anchor.TopRight)]
        [TestCase(Anchor.BottomLeft)]
        [TestCase(Anchor.BottomCentre)]
        [TestCase(Anchor.BottomRight)]
        public void TestChangeTextAnchor(Anchor anchor)
        {
            AddStep("change text anchor", () => textContainer.TextAnchor = anchor);
            AddAssert("children have correct anchors", () => textContainer.Children.All(c => c.Anchor == anchor && c.Origin == anchor));
            AddAssert("children are positioned correctly", () =>
            {
                string result = string.Concat(textContainer.Children
                                                           .OrderBy(c => c.ScreenSpaceDrawQuad.TopLeft.Y)
                                                           .ThenBy(c => c is TextFlowContainer.NewLineContainer ? 0 : c.ScreenSpaceDrawQuad.TopLeft.X)
                                                           .Select(c => (c as SpriteText)?.Text.ToString() ?? "\n"));
                return result == default_text;
            });
        }

        [Test]
        public void TestAddTextWithTextAnchor()
        {
            AddStep("change text anchor", () => textContainer.TextAnchor = Anchor.TopCentre);
            AddStep("add text", () => textContainer.AddText("added text"));
            AddAssert("children have correct anchors", () => textContainer.Children.All(c => c.Anchor == Anchor.TopCentre && c.Origin == Anchor.TopCentre));
        }

        [Test]
        public void TestSetText()
        {
            AddStep("set text", () => textContainer.Text = "first text");
            AddAssert("text flow has 2 sprite texts", () => textContainer.ChildrenOfType<SpriteText>().Count() == 2);

            AddStep("set text", () => textContainer.Text = "second text");
            AddAssert("text flow has 2 sprite texts", () => textContainer.ChildrenOfType<SpriteText>().Count() == 2);
        }

        [Test]
        public void TestPartManagement()
        {
            ITextPart part = null;

            AddStep("clear text", () => textContainer.Clear());
            assertSpriteTextCount(0);

            AddStep("add text", () => part = textContainer.AddText("this is some text"));
            AddStep("set text colour to red manually", () => part.Drawables.ForEach(p => p.Colour = Colour4.Red));
            assertSpriteTextCount(4);

            AddStep("add more text", () => textContainer.AddText("and some more of it too"));
            assertSpriteTextCount(10);

            AddStep("add manual drawable", () => textContainer.AddPart(new TextPartManual(new[]
            {
                new SpriteIcon
                {
                    Icon = FontAwesome.Regular.Clipboard,
                    Size = new Vector2(16)
                }
            })));
            assertSpriteTextCount(10);
            assertTotalChildCount(11);

            AddStep("remove original text", () => textContainer.RemovePart(part));
            assertSpriteTextCount(6);
            assertTotalChildCount(7);

            AddStep("clear text", () => textContainer.Clear());
            assertSpriteTextCount(0);
        }

        [Test]
        public void TestWordSplittingEdgeCases()
        {
            AddStep("set latin text", () => textContainer.Text = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Integer mattis eu turpis vitae posuere. Orci varius natoque penatibus et magnis dis parturient montes, nascetur ridiculus mus. Etiam mauris nibh, faucibus maximus ornare eu, ultrices ut ipsum. Proin rhoncus, nunc et faucibus pretium, nisl nunc dapibus massa, et scelerisque nibh ligula id odio. Praesent dapibus ex sed nunc egestas, in placerat risus mattis. Nulla sed ligula velit. Vestibulum auctor porta eros et condimentum. Etiam laoreet nunc nec lacinia pulvinar. Mauris hendrerit, mi at aliquet condimentum, ex ex cursus dolor, non porta erat eros id justo. Cras malesuada tincidunt nunc, at tincidunt risus eleifend id. Maecenas hendrerit venenatis mi et lobortis. Etiam sem tortor, elementum eget lacus non, porta tristique quam. Morbi sed lacinia odio. Phasellus ut pretium nunc. Fusce vitae mollis magna, vel scelerisque dui. ");
            AddStep("set url", () => textContainer.Text = "https://osu.ppy.sh/home/news/2024-03-27-osutaiko-world-cup-2024-round-of-32-recap");
            AddStep("set cjk text", () => textContainer.Text = "日本の桜は世界中から観光客を引きつけています。寿司は美味しい伝統的な日本食です。東京タワーは景色が美しいです。速い新幹線は、便利な交通手段です。富士山は、その美しさと完全な形状で知られています。日本文化は、優雅さと繊細さを象徴しています。抹茶は特別な日本の茶です。着物は、伝統的な日本の衣装で、特別な場面でよく着用されます。");
        }

        private void assertSpriteTextCount(int count)
            => AddAssert($"text flow has {count} sprite texts", () => textContainer.ChildrenOfType<SpriteText>().Count() == count);

        private void assertTotalChildCount(int count)
            => AddAssert($"text flow has {count} children", () => textContainer.Count == count);
    }
}
