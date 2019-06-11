// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Containers
{
    public class TestSceneEdgeSnappingContainer : FrameworkTestScene
    {
        public override IReadOnlyList<Type> RequiredTypes => new[] { typeof(EdgeSnappingContainer), typeof(SnapTargetContainer) };

        public TestSceneEdgeSnappingContainer()
        {
            FillFlowContainer<SnapTestContainer> container;
            Child = container = new FillFlowContainer<SnapTestContainer>
            {
                Direction = FillDirection.Horizontal,
                Margin = new MarginPadding(20),
                Spacing = new Vector2(20),
                Children = new[]
                {
                    new SnapTestContainer(Edges.Left),
                    new SnapTestContainer(Edges.Left | Edges.Top),
                    new SnapTestContainer(Edges.Top),
                    new SnapTestContainer(Edges.Top | Edges.Right),
                    new SnapTestContainer(Edges.Right),
                    new SnapTestContainer(Edges.Bottom | Edges.Right),
                    new SnapTestContainer(Edges.Bottom),
                    new SnapTestContainer(Edges.Bottom | Edges.Left),
                }
            };

            foreach (var child in container.Children)
                addBoxAssert(child);
        }

        private void addBoxAssert(SnapTestContainer container)
        {
            bool leftSnapped = container.EdgeSnappingContainer.SnappedEdges.HasFlag(Edges.Left);
            bool topSnapped = container.EdgeSnappingContainer.SnappedEdges.HasFlag(Edges.Top);
            bool rightSnapped = container.EdgeSnappingContainer.SnappedEdges.HasFlag(Edges.Right);
            bool bottomSnapped = container.EdgeSnappingContainer.SnappedEdges.HasFlag(Edges.Bottom);

            AddAssert($"\"{container.Name}\" snaps correctly", () =>
                leftSnapped == container.EdgeSnappingContainer.Padding.Left < 0
                && topSnapped == container.EdgeSnappingContainer.Padding.Top < 0
                && rightSnapped == container.EdgeSnappingContainer.Padding.Right < 0
                && bottomSnapped == container.EdgeSnappingContainer.Padding.Bottom < 0);
        }

        private class SnapTestContainer : SnapTargetContainer
        {
            internal readonly EdgeSnappingContainer EdgeSnappingContainer;

            public SnapTestContainer(Edges snappedEdges)
            {
                const float inset = 10f;
                Name = snappedEdges.ToString();
                Size = new Vector2(50);
                Children = new Drawable[]
                {
                    new Box
                    {
                        Colour = Color4.Blue,
                        RelativeSizeAxes = Axes.Both,
                    },
                    EdgeSnappingContainer = new EdgeSnappingContainer
                    {
                        SnappedEdges = snappedEdges,
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
