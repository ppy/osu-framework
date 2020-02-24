// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Layout
{
    public class TestSceneScenario1 : FrameworkTestScene
    {
        public TestSceneScenario1()
        {
            TestContainer testContainer;
            Add(testContainer = new TestContainer
            {
                RelativeSizeAxes = Axes.Both,
                Child = new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Height = 0.25f,
                    Children = new[]
                    {
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = Color4.Beige,
                            Width = 0.2f,
                        },
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = Color4.Bisque,
                            Width = 0.2f,
                        },
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = Color4.Aquamarine,
                            Width = 0.2f,
                        },
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = Color4.Cornsilk,
                            Width = 0.2f,
                        },
                    }
                }
            });

            AddSliderStep("Adjust scale", 0.5, 1.5, 1.0, b => testContainer.AdjustScale((float)b));
            AddStep("Invalidate layout", () => testContainer.Invalidate());
        }

        private class TestContainer : Container<Drawable>
        {
            public void AdjustScale(float scale = 1.0f)
            {
                this.ScaleTo(new Vector2(scale));
                this.ResizeTo(new Vector2(1 / scale));
            }
        }
    }
}
