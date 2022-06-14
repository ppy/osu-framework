// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Extensions.EnumExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Containers
{
    public class TestSceneSafeAreaOverrides : FrameworkTestScene
    {
        public TestSceneSafeAreaOverrides()
        {
            FillFlowContainer<OverrideTestContainer> container;
            Child = container = new FillFlowContainer<OverrideTestContainer>
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                AutoSizeAxes = Axes.Both,
                Direction = FillDirection.Horizontal,
                Margin = new MarginPadding(20),
                Spacing = new Vector2(20),
                Children = new[]
                {
                    new OverrideTestContainer(Edges.Left),
                    new OverrideTestContainer(Edges.Left | Edges.Top),
                    new OverrideTestContainer(Edges.Top),
                    new OverrideTestContainer(Edges.Top | Edges.Right),
                    new OverrideTestContainer(Edges.Right),
                    new OverrideTestContainer(Edges.Bottom | Edges.Right),
                    new OverrideTestContainer(Edges.Bottom),
                    new OverrideTestContainer(Edges.Bottom | Edges.Left),
                }
            };

            foreach (var child in container.Children)
                addBoxAssert(child);

            AddStep("Ensure negative size handles correctly", () =>
            {
                foreach (var child in container.Children)
                {
                    child.SafeAreaContainer.Size = -child.SafeAreaContainer.Size;
                    child.SafeAreaContainer.Position = -child.SafeAreaContainer.Position;
                }
            });
        }

        private void addBoxAssert(OverrideTestContainer container)
        {
            bool leftOverridden = container.SafeAreaContainer.SafeAreaOverrideEdges.HasFlagFast(Edges.Left);
            bool topOverridden = container.SafeAreaContainer.SafeAreaOverrideEdges.HasFlagFast(Edges.Top);
            bool rightOverridden = container.SafeAreaContainer.SafeAreaOverrideEdges.HasFlagFast(Edges.Right);
            bool bottomOverridden = container.SafeAreaContainer.SafeAreaOverrideEdges.HasFlagFast(Edges.Bottom);

            AddAssert($"\"{container.Name}\" overrides correctly", () =>
                leftOverridden == container.SafeAreaContainer.Padding.Left < 0
                && topOverridden == container.SafeAreaContainer.Padding.Top < 0
                && rightOverridden == container.SafeAreaContainer.Padding.Right < 0
                && bottomOverridden == container.SafeAreaContainer.Padding.Bottom < 0);
        }

        private class OverrideTestContainer : SafeAreaDefiningContainer
        {
            internal readonly SafeAreaContainer SafeAreaContainer;

            public OverrideTestContainer(Edges overriddenEdges)
            {
                const float inset = 10f;
                Name = overriddenEdges.ToString();
                Size = new Vector2(50);
                Children = new Drawable[]
                {
                    new Box
                    {
                        Colour = Color4.Blue,
                        RelativeSizeAxes = Axes.Both,
                    },
                    SafeAreaContainer = new SafeAreaContainer
                    {
                        SafeAreaOverrideEdges = overriddenEdges,
                        Position = new Vector2(inset),
                        Size = Size - new Vector2(inset * 2),
                        Child = new Box
                        {
                            Colour = Color4.Green,
                            RelativeSizeAxes = Axes.Both
                        }
                    }
                };
            }
        }
    }
}
