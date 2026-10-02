// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using osu.Framework.Graphics.Primitives;
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
                if (segmentCount > 0 && nodes != null)
                {
                    for (int i = firstLeafIndex; i <= lastLeafIndex; i++)
                        yield return nodes[i].Segment!.Value;
                }
            }
        }

        public RectangleF VertexBounds { get; private set; } = RectangleF.Empty;

        public int TreeVersion { get; private set; }

        private float radius;
        private BBHNode[]? nodes;
        private int treeDepth;
        private int firstLeafIndex;
        private int lastLeafIndex;
        private int segmentCount;

        public void SetVertices(IReadOnlyList<Vector2> vertices, float pathRadius)
        {
            ObjectDisposedException.ThrowIf(isDisposed, this);

            TreeVersion++;
            radius = pathRadius;

            segmentCount = Math.Max(vertices.Count - 1, 0);
            // Definition of a leaf here is a node containing a segment
            // Since we are building a binary tree, compute the max value that is bigger than segmentCount and the power of 2.
            // That would be the bottom layer of a tree.
            int maxLeafCount = Math.Max((int)System.Numerics.BitOperations.RoundUpToPowerOf2((uint)segmentCount), 1);
            treeDepth = (int)Math.Log2(maxLeafCount);

            // We can avoid storing empty nodes by computing amount of all the nodes within a tree,
            // which have descendant with at least 1 leaf (or leaf itself).
            // That would be the size of an array holding the tree
            int arrayLength = segmentCount;
            int nodesOnDepth = segmentCount;

            for (int i = treeDepth - 1; i >= 0; i--)
            {
                nodesOnDepth = (nodesOnDepth + 1) / 2;
                arrayLength += nodesOnDepth;
            }

            firstLeafIndex = arrayLength - segmentCount;
            lastLeafIndex = arrayLength - 1;

            if (nodes != null)
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
                    computeNodes(vertices);
                    VertexBounds = RectangleF.Union(nodes[0].Bounds, RectangleF.Empty);
                    break;
                }
            }
        }

        private void computeNodes(IReadOnlyList<Vector2> vertices)
        {
            Debug.Assert(nodes != null);

            for (int i = 0; i < vertices.Count - 1; i++)
            {
                var segment = new Line(vertices[i], vertices[i + 1]);

                nodes[firstLeafIndex + i] = new BBHNode
                {
                    Bounds = lineAABB(segment, radius),
                    Segment = segment
                };
            }

            if (segmentCount == 1) // At this point root must contain a segment, no parent nodes exist.
                return;

            int nodesOnCurrentDepth = segmentCount;
            int currentNodeIndex = lastLeafIndex - segmentCount;

            // iterate over the tree layers starting from the bottom
            for (int i = treeDepth - 1; i >= 0; i--)
            {
                int nodesOnNextDepth = nodesOnCurrentDepth;
                nodesOnCurrentDepth = Math.Max((nodesOnCurrentDepth + 1) / 2, 1);

                // iterate over the tree nodes within a layer from right to left
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

        /// <summary>
        /// Whether any segment of a current path contains a given point.
        /// </summary>
        /// <param name="localPos">A point in local coordinates.</param>
        public bool Contains(Vector2 localPos) => segmentCount > 0 && nodes != null && contains(localPos + VertexBounds.TopLeft, 0);

        private bool contains(Vector2 position, int? index)
        {
            if (!index.HasValue)
                return false;

            BBHNode node = nodes![index.Value];

            if (!node.Bounds.Contains(position))
                return false;

            if (node.IsLeaf)
                return node.Segment!.Value.DistanceSquaredToPoint(position) < radius * radius;

            return contains(position, node.Left) || contains(position, node.Right);
        }

        public void CollectBoundingBoxes(List<RectangleF> boxes)
        {
            boxes.Clear();

            if (segmentCount == 0 || nodes?.Length == 0)
                return;

            collectBoundingBoxes(0, boxes);
        }

        private void collectBoundingBoxes(int? index, List<RectangleF> boxes)
        {
            if (!index.HasValue)
                return;

            BBHNode node = nodes![index.Value];

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
            float maxX = line.StartPoint.X + line.EndPoint.X - minX;
            float maxY = line.StartPoint.Y + line.EndPoint.Y - minY;
            return new RectangleF(minX - radius, minY - radius, maxX - minX + radius * 2, maxY - minY + radius * 2);
        }

        private bool isDisposed;

        public void Dispose()
        {
            if (isDisposed)
                return;

            isDisposed = true;

            if (nodes != null)
                ArrayPool<BBHNode>.Shared.Return(nodes);
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
