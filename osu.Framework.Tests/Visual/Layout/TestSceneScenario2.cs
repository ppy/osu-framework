// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Layout
{
    public class TestSceneScenario2 : FrameworkTestScene
    {
        public TestSceneScenario2()
        {
            Child = new TestContainer
            {
                RelativeSizeAxes = Axes.Both,
                Size = new Vector2(0.5f),
                Child = new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Direction = FillDirection.Horizontal,
                    Children = new Drawable[]
                    {
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Width = 0.5f,
                            Colour = Color4.Red
                        },
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Width = 0.5f,
                            Colour = Color4.Green
                        },
                    }
                },
            };
        }

        private class TestContainer : CompositeDrawable
        {
            public Drawable Child
            {
                set => childContainer.Child = value;
            }

            private readonly Container childContainer;

            public TestContainer()
            {
                InternalChild = childContainer = new Container();
            }

            protected override void Update()
            {
                base.Update();

                childContainer.Size = DrawSize;
            }
        }
    }
}
