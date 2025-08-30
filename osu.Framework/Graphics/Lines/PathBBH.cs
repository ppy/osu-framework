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
    public class PathBBH : IDisposable
    {
        public IEnumerable<Line> Segments
        {
            get
            {
                if (segmentCount > 0)
                {
                    for (int i = firstLeafIndex; i <= lastLeafIndex; i++)
                        yield return nodes[i].Segment!.Value;
                }
            }
        }

        public RectangleF VertexBounds { get; private set; } = RectangleF.Empty;

        private float radius;
        private BBHNode[] nodes = null!;
        private int treeDepth;
        private int firstLeafIndex;
        private int lastLeafIndex;
        private int segmentCount;
        private bool rented;

        public void SetVertices(IReadOnlyList<Vector2> vertices, float pathRadius)
        {
            radius = pathRadius;

            segmentCount = Math.Max(vertices.Count - 1, 0);
            // Definition of a leaf here is a node containing a segment
            int maxLeafCount = Math.Max((int)System.Numerics.BitOperations.RoundUpToPowerOf2((uint)segmentCount), 1);
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
                    for (int i = 0; i < vertices.Count - 1; i++)
                    {
                        var segment = new Line(vertices[i], vertices[i + 1]);

                        nodes[firstLeafIndex + i] = new BBHNode
                        {
                            Bounds = lineAABB(segment, radius),
                            Segment = segment
                        };
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

                    // Right child exists
                    if (rightOffset <= nodesOnNextDepth)
                    {
                        int right = currentNodeIndex + rightOffset;

                        nodes[currentNodeIndex] = new BBHNode
                        {
                            Bounds = RectangleF.Union(nodes[left].Bounds, nodes[right].Bounds),
                            Left = left,
                            Right = right,
                        };
                    }
                    else
                    {
                        nodes[currentNodeIndex] = new BBHNode
                        {
                            Bounds = nodes[left].Bounds,
                            Left = left,
                        };
                    }

                    currentNodeIndex--;
                }
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

            if (!node.Bounds.Contains(position))
                return false;

            if (node.IsLeaf)
                return node.Segment!.Value.DistanceSquaredToPoint(position) < radius * radius;

            return contains(position, node.Left) || contains(position, node.Right);
        }

        public void CollectBoundingBoxes(List<RectangleF> boxes)
        {
            boxes.Clear();

            if (segmentCount == 0)
                return;

            collectBoundingBoxes(0, boxes);
        }

        private void collectBoundingBoxes(int? index, List<RectangleF> boxes)
        {
            if (!index.HasValue)
                return;

            BBHNode node = nodes[index.Value];

            boxes.Add(new RectangleF(node.Bounds.TopLeft - VertexBounds.TopLeft, node.Bounds.Size));

            if (node.IsLeaf)
                return;

            collectBoundingBoxes(node.Left, boxes);
            collectBoundingBoxes(node.Right, boxes);
        }

        private static RectangleF lineAABB(Line line, float radius)
        {
            float minX = MathUtils.BranchlessMin(line.StartPoint.X, line.EndPoint.X);
            float minY = MathUtils.BranchlessMin(line.StartPoint.Y, line.EndPoint.Y);
            float maxX = line.StartPoint.X + line.EndPoint.X - minX;
            float maxY = line.StartPoint.Y + line.EndPoint.Y - minY;
            return new RectangleF(minX - radius, minY - radius, maxX - minX + radius * 2, maxY - minY + radius * 2);
        }

        public void Dispose()
        {
            if (rented)
                ArrayPool<BBHNode>.Shared.Return(nodes);

            GC.SuppressFinalize(this);
        }

        private readonly struct BBHNode
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
            /// Whether this <see cref="BBHNode"/> contains a path segment.
            /// </summary>
            public bool IsLeaf => Segment.HasValue;

            /// <summary>
            /// The line which represents a path segment in case when this <see cref="BBHNode"/> is marked as a <see cref="IsLeaf"/>.
            /// </summary>
            public Line? Segment { get; init; }

            /// <summary>
            /// If <see cref="IsLeaf"/> - bounding box of the <see cref="Segment"/>.
            /// Otherwise - combined bounding box of <see cref="Left"/> and <see cref="Right"/> nodes.
            /// </summary>
            public required RectangleF Bounds { get; init; }
        }
    }
}
