// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Utils;
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
                    for (int i = firstLeafIndex; i <= lastLeafIndex; i++)
                        yield return new Line(nodes[i].StartPoint, nodes[i].EndPoint);
                }
            }
        }

        public Line FirstSegment => nodes[firstLimitedLeafIndex].InterpolatedSegment;
        public Line LastSegment => nodes[lastLimitedLeafIndex].InterpolatedSegment;

        public int RangeStart => firstLimitedLeafIndex - firstLeafIndex;
        public int RangeEnd => segmentCount - (lastLeafIndex - lastLimitedLeafIndex) - 1;

        public RectangleF VertexBounds { get; private set; } = RectangleF.Empty;

        public int TreeVersion { get; private set; }

        private float radius;
        private BBHNode[] nodes = [];
        private int firstLeafIndex;
        private int lastLeafIndex;
        private int firstLimitedLeafIndex;
        private int lastLimitedLeafIndex;
        private int segmentCount;
        private float totalLength;
        private float startProgress;
        private float endProgress;
        private int modifiedStartNodeIndex;
        private int modifiedEndNodeIndex;
        private Vector2 pathEndPoint;

        public void Reuse(IReadOnlyList<Vector2> vertices, float radius)
        {
            this.radius = radius;
            TreeVersion++;

            startProgress = 0;
            endProgress = 1;
            modifiedStartNodeIndex = -1;
            modifiedEndNodeIndex = -1;
            totalLength = 0;
            pathEndPoint = Vector2.Zero;

            segmentCount = Math.Max(vertices.Count - 1, 0);
            // Definition of a leaf here is a node containing a segment
            int maxLeafCount = Math.Max(smallestPowerOfTwo(segmentCount), 1);
            firstLeafIndex = firstLimitedLeafIndex = maxLeafCount - 1;
            lastLeafIndex = lastLimitedLeafIndex = firstLeafIndex + (segmentCount - 1);

            if (nodes.Length != lastLeafIndex + 1)
                nodes = new BBHNode[lastLeafIndex + 1];

            switch (vertices.Count)
            {
                case 0:
                    VertexBounds = RectangleF.Empty;
                    break;

                case 1:
                    pathEndPoint = vertices[0];
                    VertexBounds = RectangleF.Union(new RectangleF(vertices[0] - new Vector2(radius), new Vector2(radius * 2)), RectangleF.Empty);
                    break;

                default:
                {
                    pathEndPoint = vertices[^1];
                    int leafOffset = 0;

                    for (int i = 0; i < vertices.Count - 1; i++)
                    {
                        var segment = new Line(vertices[i], vertices[i + 1]);
                        totalLength += segment.Rho;

                        nodes[firstLeafIndex + leafOffset] = new BBHNode
                        {
                            Bounds = lineAABB(segment, radius),
                            StartPoint = segment.StartPoint,
                            EndPoint = segment.EndPoint,
                            CumulativeLength = totalLength,
                            IsLeaf = true
                        };

                        leafOffset++;
                    }

                    computeParentNodes();

                    VertexBounds = union(nodes[0].Bounds, RectangleF.Empty) ?? RectangleF.Empty;
                    break;
                }
            }
        }

        public float StartProgress
        {
            get => startProgress;
            set
            {
                if (startProgress == value)
                    return;

                startProgress = Math.Clamp(value, 0, endProgress - float.Epsilon);
                updateStartProgress(startProgress);
            }
        }

        public float EndProgress
        {
            get => endProgress;
            set
            {
                if (endProgress == value)
                    return;

                endProgress = Math.Clamp(value, startProgress + float.Epsilon, 1);
                updateEndProgress(endProgress);
            }
        }

        private void updateStartProgress(float newStart)
        {
            if (modifiedStartNodeIndex != -1)
            {
                int n = modifiedStartNodeIndex;
                nodes[n].InterpolatedSegmentStart = null;
                nodes[n].Bounds = lineAABB(nodes[n].InterpolatedSegment, radius);

                if (n != 0)
                {
                    while (true)
                    {
                        n = (n - 1) / 2;
                        int left = 2 * n + 1;
                        int right = 2 * n + 2;

                        nodes[left].Disabled = false;
                        nodes[n].Bounds = union(nodes[left].Bounds, right > lastLeafIndex || nodes[right].Disabled ? null : nodes[right].Bounds);

                        if (n == 0)
                            break;
                    }
                }

                firstLimitedLeafIndex = firstLeafIndex;
                modifiedStartNodeIndex = -1;
            }

            if (newStart == 0)
            {
                VertexBounds = union(nodes[0].Bounds, RectangleF.Empty) ?? RectangleF.Empty;
                return;
            }

            float newStartLength = totalLength * newStart;
            int i = 0;

            while (true)
            {
                if (i > lastLeafIndex)
                    break;

                if (nodes[i].IsLeaf)
                {
                    float segmentLength = i == 0 ? nodes[i].CumulativeLength : nodes[i].CumulativeLength - (i > firstLeafIndex ? nodes[i - 1].CumulativeLength : 0);
                    float newSegmentLength = nodes[i].CumulativeLength - newStartLength;
                    nodes[i].InterpolatedSegmentStart = Precision.AlmostEquals(segmentLength, 0) ? nodes[i].EndPoint : Interpolation.ValueAt(newSegmentLength / segmentLength, nodes[i].EndPoint, nodes[i].StartPoint, 0, 1);
                    nodes[i].Bounds = lineAABB(nodes[i].InterpolatedSegment, radius);
                    firstLimitedLeafIndex = modifiedStartNodeIndex = i;
                    break;
                }

                int right = 2 * i + 2;
                int left = 2 * i + 1;

                float leftLength = nodes[left].CumulativeLength;

                if (newStartLength > leftLength)
                    i = right;
                else
                    i = left;
            }

            if (modifiedStartNodeIndex == -1)
            {
                VertexBounds = union(nodes[0].Bounds, RectangleF.Empty) ?? RectangleF.Empty;
                return;
            }

            i = modifiedStartNodeIndex;

            if (i != 0)
            {
                while (true)
                {
                    int modifiedChild = i;
                    i = (i - 1) / 2;
                    int left = 2 * i + 1;
                    int right = 2 * i + 2;

                    if (modifiedChild == left)
                    {
                        nodes[i].Bounds = right > lastLeafIndex || nodes[right].Disabled ? nodes[left].Bounds : union(nodes[left].Bounds, nodes[right].Bounds);
                    }
                    else
                    {
                        nodes[left].Disabled = true;
                        nodes[i].Bounds = nodes[right].Bounds;
                    }

                    if (i == 0)
                        break;
                }
            }

            VertexBounds = union(nodes[0].Bounds, RectangleF.Empty) ?? RectangleF.Empty;
        }

        private void updateEndProgress(float newEnd)
        {
            if (modifiedEndNodeIndex != -1)
            {
                int n = modifiedEndNodeIndex;
                nodes[n].InterpolatedSegmentEnd = null;
                nodes[n].Bounds = lineAABB(nodes[n].InterpolatedSegment, radius);

                if (n != 0)
                {
                    while (true)
                    {
                        n = (n - 1) / 2;
                        int left = 2 * n + 1;
                        int right = 2 * n + 2;

                        if (right <= lastLeafIndex)
                            nodes[right].Disabled = false;

                        nodes[n].Bounds = union(nodes[left].Disabled ? null : nodes[left].Bounds, right > lastLeafIndex ? null : nodes[right].Bounds);

                        if (n == 0)
                            break;
                    }
                }

                lastLimitedLeafIndex = lastLeafIndex;
                modifiedEndNodeIndex = -1;
            }

            if (newEnd == 1)
            {
                VertexBounds = union(nodes[0].Bounds, RectangleF.Empty) ?? RectangleF.Empty;
                return;
            }

            float newEndLength = totalLength * newEnd;
            int i = 0;

            while (true)
            {
                if (i > lastLeafIndex)
                    break;

                if (nodes[i].IsLeaf)
                {
                    float segmentLength = i == 0 ? nodes[i].CumulativeLength : nodes[i].CumulativeLength - ((i > firstLeafIndex) ? nodes[i - 1].CumulativeLength : 0);
                    float newSegmentLength = nodes[i].CumulativeLength - newEndLength;
                    nodes[i].InterpolatedSegmentEnd = Precision.AlmostEquals(segmentLength, 0) ? nodes[i].StartPoint : Interpolation.ValueAt(newSegmentLength / segmentLength, nodes[i].EndPoint, nodes[i].StartPoint, 0, 1);
                    nodes[i].Bounds = lineAABB(nodes[i].InterpolatedSegment, radius);
                    lastLimitedLeafIndex = modifiedEndNodeIndex = i;
                    break;
                }

                int right = 2 * i + 2;
                int left = 2 * i + 1;

                float leftLength = nodes[left].CumulativeLength;

                if (newEndLength < leftLength)
                    i = left;
                else
                    i = right;
            }

            if (modifiedEndNodeIndex == -1)
            {
                VertexBounds = union(nodes[0].Bounds, RectangleF.Empty) ?? RectangleF.Empty;
                return;
            }

            i = modifiedEndNodeIndex;

            if (i != 0)
            {
                while (true)
                {
                    int modifiedChild = i;
                    i = (i - 1) / 2;
                    int left = 2 * i + 1;
                    int right = 2 * i + 2;

                    if (modifiedChild == right)
                    {
                        nodes[i].Bounds = nodes[left].Disabled ? nodes[right].Bounds : union(nodes[left].Bounds, nodes[right].Bounds);
                    }
                    else
                    {
                        if (right <= lastLeafIndex)
                            nodes[right].Disabled = true;

                        nodes[i].Bounds = nodes[left].Bounds;
                    }

                    if (i == 0)
                        break;
                }
            }

            VertexBounds = union(nodes[0].Bounds, RectangleF.Empty) ?? RectangleF.Empty;
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
                int i = stack.Pop();
                if (i > lastLeafIndex)
                    continue;

                var node = nodes[i];

                if (node.Disabled || !node.Bounds.HasValue || !node.Bounds.Value.Contains(pos))
                    continue;

                if (node.IsLeaf && node.InterpolatedSegment.DistanceSquaredToPoint(pos) < radius * radius)
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
                    Bounds = right > lastLeafIndex ? nodes[left].Bounds : union(nodes[left].Bounds, nodes[right].Bounds),
                    Left = left,
                    Right = right,
                    CumulativeLength = Math.Max(nodes[left].CumulativeLength, right > lastLeafIndex ? totalLength : nodes[right].CumulativeLength),
                    StartPoint = nodes[left].StartPoint,
                    EndPoint = right > lastLeafIndex ? pathEndPoint : nodes[right].EndPoint
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

            if (node.Disabled || node.Bounds is not RectangleF bounds)
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

        private struct BBHNode
        {
            public int? Left { get; init; }
            public int? Right { get; init; }

            public bool Disabled { get; set; }

            public bool IsLeaf { get; init; }

            public Line InterpolatedSegment => new Line(InterpolatedSegmentStart ?? StartPoint, InterpolatedSegmentEnd ?? EndPoint);

            public Vector2 StartPoint { get; init; }
            public Vector2 EndPoint { get; init; }

            public Vector2? InterpolatedSegmentStart { get; set; }
            public Vector2? InterpolatedSegmentEnd { get; set; }

            public float CumulativeLength { get; init; }

            public RectangleF? Bounds { get; set; }
        }

        private static RectangleF? union(RectangleF? left, RectangleF? right)
        {
            if (left.HasValue && right.HasValue)
                return RectangleF.Union(left.Value, right.Value);

            return left ?? right;
        }
    }
}
