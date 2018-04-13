// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
using OpenTK.Graphics;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Testing;

namespace osu.Framework.Tests.Visual
{
    public class TestCaseDrawSizePreservingFillContainer : TestCase
    {
        public TestCaseDrawSizePreservingFillContainer()
        {
            DrawSizePreservingFillContainer fillContainer;
            Child = new Container
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Size = new Vector2(500),
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.Red,
                    },
                    new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                        Padding = new MarginPadding(10),
                        Children = new Drawable[]
                        {
                            new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Colour = Color4.Black,
                            },
                            fillContainer = new DrawSizePreservingFillContainer
                            {
                                Child = new TestCaseSizing(),
                            },
                        }
                    },
                }
            };

            AddStep("Strategy: Minimum", () => fillContainer.Strategy = DrawSizePreservationStrategy.Minimum);
            AddStep("Strategy: Maximum", () => fillContainer.Strategy = DrawSizePreservationStrategy.Maximum);
            AddStep("Strategy: Average", () => fillContainer.Strategy = DrawSizePreservationStrategy.Average);
            AddStep("Strategy: Separate", () => fillContainer.Strategy = DrawSizePreservationStrategy.Separate);

            AddSliderStep("Width", 50, 650, 500, v => Child.Width = v);
            AddSliderStep("Height", 50, 650, 500, v => Child.Height = v);

            AddStep("Override Size to 1x1", () => Child.Size = Vector2.One);
        }
    }
}
