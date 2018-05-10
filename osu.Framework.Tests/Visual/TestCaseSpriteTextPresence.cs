// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using NUnit.Framework;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.MathUtils;
using OpenTK.Graphics;

namespace osu.Framework.Tests.Visual
{
    public class TestCaseSpriteTextPresence : FrameworkTestCase
    {
        /// <summary>
        /// Tests with a normal <see cref="SpriteText"/> which changes presence based on whether text is empty.
        /// </summary>
        [Test]
        public void TestNormalSpriteText()
        {
            Container container = null;
            SpriteText text = null;

            AddStep("reset", () =>
            {
                Child = container = new Container
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    AutoSizeAxes = Axes.Both,
                    Children = new Drawable[]
                    {
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = Color4.Red.Opacity(0.3f)
                        },
                        text = new SpriteText
                        {
                            Text = "Hello world!",
                            TextSize = 12,
                        }
                    }
                };
            });

            AddAssert("is present", () => text.IsPresent);
            AddAssert("height == 12", () => Precision.AlmostEquals(12, container.Height));
            AddStep("empty text", () => text.Text = string.Empty);
            AddAssert("not present", () => !text.IsPresent);
            AddAssert("height == 0", () => Precision.AlmostEquals(0, container.Height));
        }

        /// <summary>
        /// Tests with a special <see cref="SpriteText"/> that always remains present regardless of whether text is empty.
        /// </summary>
        [Test]
        public void TestAlwaysPresentSpriteText()
        {
            Container container = null;
            SpriteText text = null;

            AddStep("reset", () =>
            {
                Child = container = new Container
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    AutoSizeAxes = Axes.Both,
                    Children = new Drawable[]
                    {
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = Color4.Red.Opacity(0.3f)
                        },
                        text = new AlwaysPresentSpriteText
                        {
                            Text = "Hello world!",
                            TextSize = 12,
                        }
                    }
                };
            });

            AddAssert("is present", () => text.IsPresent);
            AddAssert("height == 12", () => Precision.AlmostEquals(12, container.Height));
            AddStep("empty text", () => text.Text = string.Empty);
            AddAssert("is present", () => text.IsPresent);
            AddAssert("height == 0", () => Precision.AlmostEquals(0, container.Height));
        }

        private class AlwaysPresentSpriteText : SpriteText
        {
            public override bool IsPresent => true;
        }
    }
}
