// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Layout
{
    public class TestSceneScenario3 : FrameworkTestScene
    {
        private Container<Drawable> content;

        [SetUp]
        public void Setup() => Schedule(() =>
        {
            Child = new Container
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                AutoSizeAxes = Axes.Y,
                Width = 500,
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.Green
                    },
                    content = new Container
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Padding = new MarginPadding(10)
                    }
                }
            };
        });

        [Test]
        public void TestAddChild()
        {
            AddStep("add child", () => LoadComponentAsync(new Box
            {
                RelativeSizeAxes = Axes.X,
                Height = 60,
            }, d =>
            {
                content.Add(d);
                d.FadeInFromZero(500);
            }));
        }
    }
}
