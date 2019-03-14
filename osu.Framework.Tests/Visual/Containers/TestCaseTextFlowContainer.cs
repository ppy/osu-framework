// Copyright (c) 2007-2019 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Linq;
using NUnit.Framework;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Testing;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Containers
{
    public class TestCaseTextFlowContainer : TestCase
    {
        private const string default_text = "Default text";

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
            AddAssert("children are in correct order", () =>
            {
                var children = textContainer.FlowingChildren.OfType<SpriteText>().Select(c => c.Text);

                if ((anchor & Anchor.x2) > 0)
                    children = children.Reverse();

                return children.Aggregate((cur, next) => cur + next) == default_text;
            });
        }
    }
}
