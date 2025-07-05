// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Graphics.Primitives;
using osuTK;

namespace osu.Framework.Graphics.Lines
{
    public class PathBBH
    {
        public IEnumerable<Line> Segments
        {
            get
            {
                if (segmentCount > 0)
                {
                    Vector2 offset = VertexBounds.TopLeft;

                    for (int i = firstLeafIndex; i <= lastLeafIndex; i++)
                    {
                        if (nodes[i].Segment is Line s)
                            yield return new Line(s.StartPoint - offset, s.EndPoint - offset);
                    }
                }
            }
        }

        public RectangleF VertexBounds { get; private set; } = RectangleF.Empty;

        private float radius;
        private BBHNode[] nodes = [];
        private int totalNodeCount;
        private int firstLeafIndex;
        private int lastLeafIndex;
        private int segmentCount;

        public void Reuse(IReadOnlyList<Vector2> vertices, float radius)
        {
            this.radius = radius;

            segmentCount = Math.Max(vertices.Count - 1, 0);
            // Definition of a leaf here is a node containing a segment
            int maxLeafCount = Math.Max(smallestPowerOfTwo(segmentCount), 1);
            firstLeafIndex = maxLeafCount - 1;
            lastLeafIndex = firstLeafIndex + (segmentCount - 1);
            totalNodeCount = lastLeafIndex + 1;

            if (nodes.Length != totalNodeCount)
                nodes = new BBHNode[totalNodeCount];

            switch (vertices.Count)
            {
                case 0:
                    VertexBounds = RectangleF.Empty;
                    break;

                case 1:
                    VertexBounds = union(new RectangleF(vertices[0] - new Vector2(radius), new Vector2(radius * 2)), RectangleF.Empty) ?? RectangleF.Empty;
                    break;

                default:
                {
                    int leafOffset = 0;

                    for (int i = 0; i < vertices.Count - 1; i++)
                    {
                        var segment = new Line(vertices[i], vertices[i + 1]);

                        nodes[firstLeafIndex + leafOffset] = new BBHNode
                        {
                            Bounds = lineAABB(segment, radius),
                            Segment = segment,
                        };

                        leafOffset++;
                    }

                    computeParentNodes();

                    VertexBounds = union(nodes[0].Bounds, RectangleF.Empty) ?? RectangleF.Empty;
                    break;
                }
            }
        }

        public bool Contains(Vector2 pos)
        {
            if (segmentCount == 0)
                return false;

            Stack<int> stack = new Stack<int>();
            stack.Push(0);

            pos += VertexBounds.TopLeft;

            while (stack.Count > 0)
            {
                int index = stack.Pop();
                if (index > totalNodeCount - 1)
                    continue;

                var node = nodes[index];

                if (!(node.Bounds?.Contains(pos) ?? false))
                    continue;

                if (node.Segment.HasValue && node.Segment.Value.DistanceToPoint(pos) < radius)
                    return true;

                if (node.Left.HasValue)
                    stack.Push(node.Left.Value);

                if (node.Right.HasValue)
                    stack.Push(node.Right.Value);
            }

            return false;
        }

        private void computeParentNodes()
        {
            if (lastLeafIndex == 0) // bounds are already computed for a node containing a segment
                return;

            for (int i = (lastLeafIndex - 1) / 2; i >= 0; i--)
            {
                int left = 2 * i + 1;
                int right = 2 * i + 2;

                nodes[i] = new BBHNode
                {
                    Bounds = union(left > lastLeafIndex ? null : nodes[left].Bounds, right > lastLeafIndex ? null : nodes[right].Bounds),
                    Left = left,
                    Right = right
                };
            }
        }

        public void CollectBoundingBoxes(List<RectangleF> boxes)
        {
            if (segmentCount == 0)
                return;

            collectBoundingBoxes(0, boxes);
        }

        private void collectBoundingBoxes(int? index, List<RectangleF> boxes)
        {
            if (index is not int i)
                return;

            if (i > lastLeafIndex)
                return;

            BBHNode node = nodes[i];

            if (node.Bounds is not RectangleF bounds)
                return;

            boxes.Add(new RectangleF(bounds.TopLeft - VertexBounds.TopLeft, bounds.Size));

            collectBoundingBoxes(node.Left, boxes);
            collectBoundingBoxes(node.Right, boxes);
        }

        private static RectangleF lineAABB(Line line, float radius)
        {
            float minX = Math.Min(line.StartPoint.X, line.EndPoint.X);
            float minY = Math.Min(line.StartPoint.Y, line.EndPoint.Y);
            float maxX = Math.Max(line.StartPoint.X, line.EndPoint.X);
            float maxY = Math.Max(line.StartPoint.Y, line.EndPoint.Y);
            return new RectangleF(minX - radius, minY - radius, maxX - minX + radius * 2, maxY - minY + radius * 2);
        }

        private static int smallestPowerOfTwo(int n)
        {
            if (n == 0)
                return 1;

            n -= 1;
            n |= n >> 1;
            n |= n >> 2;
            n |= n >> 4;
            n |= n >> 8;
            n |= n >> 16;

            return n + 1;
        }

        private readonly struct BBHNode
        {
            public int? Left { get; init; }
            public int? Right { get; init; }

            public Line? Segment { get; init; }

            public RectangleF? Bounds { get; init; }
        }

        private static RectangleF? union(RectangleF? left, RectangleF? right)
        {
            if (left.HasValue && right.HasValue)
                return RectangleF.Union(left.Value, right.Value);

            return left ?? right;
        }
    }
}
