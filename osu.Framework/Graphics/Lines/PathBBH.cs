// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Buffers;
using System.Collections.Generic;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Utils;
using osuTK;

namespace osu.Framework.Graphics.Lines
{
    /// <summary>
    /// A Bounding Box Hierarchy of a set of vertices which when drawn consecutively represent a path.
    /// </summary>
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

        private float startProgress;

        public float StartProgress
        {
            get => startProgress;
            set
            {
                if (startProgress == value || segmentCount == 0)
                    return;

                startProgress = Math.Clamp(value, 0, endProgress - float.Epsilon);
                updateStartProgress(startProgress);
                VertexBounds = RectangleF.Union(nodes[0].Bounds, RectangleF.Empty);
            }
        }

        private float endProgress;

        public float EndProgress
        {
            get => endProgress;
            set
            {
                if (endProgress == value || segmentCount == 0)
                    return;

                endProgress = Math.Clamp(value, startProgress + float.Epsilon, 1);
                updateEndProgress(endProgress);
                VertexBounds = RectangleF.Union(nodes[0].Bounds, RectangleF.Empty);
            }
        }

        public int TreeVersion { get; private set; }

        public Line FirstSegment => nodes[modifiedStartNodeIndex ?? firstLeafIndex].CurrentSegment;
        public Line LastSegment => nodes[modifiedEndNodeIndex ?? lastLeafIndex].CurrentSegment;

        public int RangeStart => (modifiedStartNodeIndex ?? firstLeafIndex) - firstLeafIndex;
        public int RangeEnd => segmentCount - (lastLeafIndex - (modifiedEndNodeIndex ?? lastLeafIndex)) - 1;

        public RectangleF VertexBounds { get; private set; } = RectangleF.Empty;

        private float radius;
        private BBHNode[] nodes = null!;
        private int treeDepth;
        private int firstLeafIndex;
        private int lastLeafIndex;
        private int segmentCount;
        private float totalLength;
        private int? modifiedStartNodeIndex;
        private int? modifiedEndNodeIndex;
        private bool rented;

        public void SetVertices(IReadOnlyList<Vector2> vertices, float pathRadius)
        {
            radius = pathRadius;
            TreeVersion++;

            startProgress = 0;
            endProgress = 1;
            modifiedStartNodeIndex = null;
            modifiedEndNodeIndex = null;
            totalLength = 0;

            segmentCount = Math.Max(vertices.Count - 1, 0);
            // Definition of a leaf here is a node containing a segment
            int maxLeafCount = Math.Max(smallestPowerOfTwo(segmentCount), 1);
            treeDepth = (int)Math.Log2(maxLeafCount);
            int arrayLength = segmentCount;

            int nodesOnDepth = segmentCount;

            for (int i = treeDepth - 1; i >= 0; i--)
            {
                nodesOnDepth = (nodesOnDepth + 1) / 2;
                arrayLength += nodesOnDepth;
            }

            firstLeafIndex = arrayLength - segmentCount;
            lastLeafIndex = arrayLength - 1;

            if (rented)
            {
                if (nodes.Length < arrayLength)
                {
                    ArrayPool<BBHNode>.Shared.Return(nodes);
                    nodes = ArrayPool<BBHNode>.Shared.Rent(arrayLength);
                }
            }
            else
            {
                nodes = ArrayPool<BBHNode>.Shared.Rent(arrayLength);
                rented = true;
            }

            switch (vertices.Count)
            {
                case 0:
                    VertexBounds = RectangleF.Empty;
                    break;

                case 1:
                    VertexBounds = RectangleF.Union(new RectangleF(vertices[0] - new Vector2(radius), new Vector2(radius * 2)), RectangleF.Empty);
                    break;

                default:
                {
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

                    VertexBounds = RectangleF.Union(nodes[0].Bounds, RectangleF.Empty);
                    break;
                }
            }
        }

        private void computeParentNodes()
        {
            if (lastLeafIndex == 0) // bounds are already computed for a node containing a segment
                return;

            int nodesOnCurrentDepth = segmentCount;
            int currentNodeIndex = lastLeafIndex - segmentCount;

            for (int i = treeDepth - 1; i >= 0; i--)
            {
                int nodesOnNextDepth = nodesOnCurrentDepth;
                nodesOnCurrentDepth = Math.Max((nodesOnCurrentDepth + 1) / 2, 1);

                for (int j = nodesOnCurrentDepth - 1; j >= 0; j--)
                {
                    int offset = (nodesOnCurrentDepth - j) + 2 * j;
                    int left = currentNodeIndex + offset;
                    int rightOffset = offset + 1;
                    int? right = rightOffset > nodesOnNextDepth ? null : (currentNodeIndex + rightOffset);

                    nodes[currentNodeIndex] = new BBHNode
                    {
                        Bounds = right.HasValue ? RectangleF.Union(nodes[left].Bounds, nodes[right.Value].Bounds) : nodes[left].Bounds,
                        Left = left,
                        Right = right,
                        CumulativeLength = Math.Max(nodes[left].CumulativeLength, right.HasValue ? nodes[right.Value].CumulativeLength : totalLength)
                    };

                    nodes[left].Parent = currentNodeIndex;

                    if (right.HasValue)
                        nodes[right.Value].Parent = currentNodeIndex;

                    currentNodeIndex--;
                }
            }
        }

        private void updateStartProgress(float newStartProgress)
        {
            if (modifiedStartNodeIndex.HasValue)
            {
                int n = modifiedStartNodeIndex.Value;
                nodes[n].InterpolatedSegmentStart = null;
                nodes[n].Bounds = lineAABB(nodes[n].CurrentSegment, radius);

                while (true)
                {
                    if (nodes[n].Parent is not int parent)
                        break;

                    n = parent;
                    int left = nodes[n].Left;
                    int? right = nodes[n].Right;

                    nodes[left].Disabled = false;
                    nodes[n].Bounds = !right.HasValue || nodes[right.Value].Disabled ? nodes[left].Bounds : RectangleF.Union(nodes[left].Bounds, nodes[right.Value].Bounds);
                }

                modifiedStartNodeIndex = null;
            }

            if (newStartProgress == 0)
                return;

            var positionAt = CurvePositionAt(newStartProgress);

            if (!positionAt.HasValue)
                return;

            nodes[positionAt.Value.index].InterpolatedSegmentStart = positionAt.Value.position;
            nodes[positionAt.Value.index].Bounds = lineAABB(nodes[positionAt.Value.index].CurrentSegment, radius);
            modifiedStartNodeIndex = positionAt.Value.index;

            int i = modifiedStartNodeIndex.Value;

            while (true)
            {
                if (nodes[i].Parent is not int parent)
                    break;

                int modifiedChild = i;
                i = parent;
                int left = nodes[i].Left;
                int? right = nodes[i].Right;

                if (modifiedChild == left)
                {
                    nodes[i].Bounds = !right.HasValue || nodes[right.Value].Disabled ? nodes[left].Bounds : RectangleF.Union(nodes[left].Bounds, nodes[right.Value].Bounds);
                }
                else
                {
                    nodes[left].Disabled = true;
                    nodes[i].Bounds = nodes[right!.Value].Bounds;
                }
            }
        }

        private void updateEndProgress(float newEndProgress)
        {
            if (modifiedEndNodeIndex.HasValue)
            {
                int n = modifiedEndNodeIndex.Value;
                nodes[n].InterpolatedSegmentEnd = null;
                nodes[n].Bounds = lineAABB(nodes[n].CurrentSegment, radius);

                while (true)
                {
                    if (nodes[n].Parent is not int parent)
                        break;

                    n = parent;
                    int left = nodes[n].Left;
                    int? right = nodes[n].Right;

                    if (right.HasValue)
                        nodes[right.Value].Disabled = false;

                    nodes[n].Bounds = nodes[left].Disabled ? nodes[right!.Value].Bounds : (!right.HasValue ? nodes[left].Bounds : RectangleF.Union(nodes[left].Bounds, nodes[right.Value].Bounds));
                }

                modifiedEndNodeIndex = null;
            }

            if (newEndProgress == 1)
                return;

            var positionAt = CurvePositionAt(newEndProgress);

            if (!positionAt.HasValue)
                return;

            nodes[positionAt.Value.index].InterpolatedSegmentEnd = positionAt.Value.position;
            nodes[positionAt.Value.index].Bounds = lineAABB(nodes[positionAt.Value.index].CurrentSegment, radius);
            modifiedEndNodeIndex = positionAt.Value.index;

            int i = modifiedEndNodeIndex.Value;

            while (true)
            {
                if (nodes[i].Parent is not int parent)
                    break;

                int modifiedChild = i;
                i = parent;
                int left = nodes[i].Left;
                int? right = nodes[i].Right;

                if (modifiedChild == right)
                {
                    nodes[i].Bounds = nodes[left].Disabled ? nodes[right.Value].Bounds : RectangleF.Union(nodes[left].Bounds, nodes[right.Value].Bounds);
                }
                else
                {
                    if (right.HasValue)
                        nodes[right.Value].Disabled = true;

                    nodes[i].Bounds = nodes[left].Bounds;
                }
            }
        }

        public (int index, Vector2 position)? CurvePositionAt(float progress)
        {
            if (segmentCount == 0)
                return null;

            if (progress == 0)
                return (firstLeafIndex, nodes[firstLeafIndex].StartPoint);

            if (progress == 1)
                return (lastLeafIndex, nodes[lastLeafIndex].EndPoint);

            float lengthAtProgress = totalLength * progress;
            int i = 0;

            while (true)
            {
                if (nodes[i].IsLeaf)
                {
                    float segmentLength = nodes[i].CumulativeLength - (i > firstLeafIndex ? nodes[i - 1].CumulativeLength : 0);
                    float lengthFromEnd = nodes[i].CumulativeLength - lengthAtProgress;
                    return (i, Precision.AlmostEquals(segmentLength, 0) ? nodes[i].EndPoint : nodes[i].EndPoint + (nodes[i].StartPoint - nodes[i].EndPoint) * lengthFromEnd / segmentLength);
                }

                int left = nodes[i].Left;
                int? right = nodes[i].Right;

                if (lengthAtProgress > nodes[left].CumulativeLength)
                {
                    if (right.HasValue)
                        i = right.Value;
                    else
                        return (lastLeafIndex, nodes[lastLeafIndex].EndPoint);
                }
                else
                    i = left;
            }
        }

        public bool Contains(Vector2 pos)
        {
            if (segmentCount == 0)
                return false;

            return contains(pos + VertexBounds.TopLeft, 0);
        }

        private bool contains(Vector2 position, int? index)
        {
            if (!index.HasValue)
                return false;

            BBHNode node = nodes[index.Value];

            if (node.Disabled || !node.Bounds.Contains(position))
                return false;

            if (node.IsLeaf)
                return node.CurrentSegment.DistanceSquaredToPoint(position) < radius * radius;

            return contains(position, node.Left) || contains(position, node.Right);
        }

        public void CollectBoundingBoxes(List<RectangleF> boxes)
        {
            if (segmentCount == 0)
                return;

            collectBoundingBoxes(0, boxes);
        }

        private void collectBoundingBoxes(int? index, List<RectangleF> boxes)
        {
            if (!index.HasValue)
                return;

            BBHNode node = nodes[index.Value];

            if (node.Disabled)
                return;

            boxes.Add(new RectangleF(node.Bounds.TopLeft - VertexBounds.TopLeft, node.Bounds.Size));

            if (node.IsLeaf)
                return;

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

        /// <summary>
        /// Returns first smallest number of power of 2 that is bigger than input.
        /// </summary>
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

        public void FreeArray()
        {
            if (rented)
                ArrayPool<BBHNode>.Shared.Return(nodes);
        }

        private struct BBHNode
        {
            /// <summary>
            /// Index of a left child of this <see cref="BBHNode"/> in the tree array.
            /// </summary>
            public int Left { get; init; }

            /// <summary>
            /// Index of a right child of this <see cref="BBHNode"/> in the tree array. Null if no right child exists.
            /// </summary>
            public int? Right { get; init; }

            /// <summary>
            /// Index of a parent of this <see cref="BBHNode"/> in the tree array.
            /// </summary>
            public int? Parent { get; set; }

            /// <summary>
            /// Whether this <see cref="BBHNode"/> should not be considered for bounding box calculations.
            /// </summary>
            public bool Disabled { get; set; }

            /// <summary>
            /// Whether this <see cref="BBHNode"/> contains a path segment.
            /// </summary>
            public bool IsLeaf { get; init; }

            /// <summary>
            /// The line which represents a (modified) path segment in case when this <see cref="BBHNode"/> is marked as a <see cref="IsLeaf"/>.
            /// </summary>
            public Line CurrentSegment => new Line(InterpolatedSegmentStart ?? StartPoint, InterpolatedSegmentEnd ?? EndPoint);

            /// <summary>
            /// Start position of a segment of this <see cref="BBHNode"/>.
            /// </summary>
            public Vector2 StartPoint { get; init; }

            /// <summary>
            /// End position of a segment of this <see cref="BBHNode"/>.
            /// </summary>
            public Vector2 EndPoint { get; init; }

            /// <summary>
            /// Modified start point of a segment of this <see cref="BBHNode"/>. Returns null if no such modification has taken place.
            /// </summary>
            public Vector2? InterpolatedSegmentStart { get; set; }

            /// <summary>
            /// Modified end point of a segment of this <see cref="BBHNode"/>. Returns null if no such modification has taken place.
            /// </summary>
            public Vector2? InterpolatedSegmentEnd { get; set; }

            /// <summary>
            /// If <see cref="IsLeaf"/> - sum of lengths of all the <see cref="CurrentSegment"/>s (including the segment of this <see cref="BBHNode"/>) to the left of this <see cref="BBHNode"/>.
            /// Otherwise - max <see cref="CumulativeLength"/> between <see cref="Left"/> and <see cref="Right"/> nodes.
            /// </summary>
            public required float CumulativeLength { get; init; }

            /// <summary>
            /// If <see cref="IsLeaf"/> - bounding box of the <see cref="CurrentSegment"/>.
            /// Otherwise - combined bounding box of <see cref="Left"/> and <see cref="Right"/> nodes.
            /// </summary>
            public required RectangleF Bounds { get; set; }
        }
    }
}
