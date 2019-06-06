// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Containers
{
    public class TestSceneDrawSizePreservingFillContainer : FrameworkTestScene
    {
        public TestSceneDrawSizePreservingFillContainer()
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
                                Child = new TestSceneSizing(),
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
