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
    public class TestSceneTextFlowContainer : FrameworkTestScene
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

        private void assertSpriteTextCount(int count)
            => AddAssert($"text flow has {count} sprite texts", () => textContainer.ChildrenOfType<SpriteText>().Count() == count);

        private void assertTotalChildCount(int count)
            => AddAssert($"text flow has {count} children", () => textContainer.Count == count);
    }
}
