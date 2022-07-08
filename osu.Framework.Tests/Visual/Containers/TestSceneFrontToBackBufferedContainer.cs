// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Containers
{
    public class TestSceneFrontToBackBufferedContainer : FrameworkTestScene
    {
        [Test]
        public void TestBufferedContainerBehindBox()
        {
            AddStep("set children", () =>
            {
                Children = new Drawable[]
                {
                    new TestBufferedContainer(true)
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.BottomCentre,
                        Size = new Vector2(200)
                    },
                    new Box
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Colour = Color4.SlateGray,
                        Size = new Vector2(300),
                    },
                };
            });
        }

        [Test]
        public void TestBufferedContainerAboveBox()
        {
            AddStep("set children", () =>
            {
                Children = new Drawable[]
                {
                    new Box
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Colour = Color4.SlateGray,
                        Size = new Vector2(300),
                    },
                    new TestBufferedContainer(false)
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.BottomCentre,
                        Size = new Vector2(200)
                    },
                };
            });
        }

        public class TestBufferedContainer : BufferedContainer
        {
            public TestBufferedContainer(bool behind)
            {
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.Orange
                    },
                    new SpriteText
                    {
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        Y = 5,
                        Text = $"Behind = {behind}"
                    }
                };
            }
        }
    }
}
