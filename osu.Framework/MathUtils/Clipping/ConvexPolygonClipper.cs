// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.CompilerServices;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Primitives;
using osuTK;

namespace osu.Framework.MathUtils.Clipping
{
    public readonly ref struct ConvexPolygonClipper<TSubject, TClip>
        where TSubject : IConvexPolygon
        where TClip : IConvexPolygon
    {
        private readonly TClip clipPolygon;
        private readonly TSubject subjectPolygon;

        public ConvexPolygonClipper(TClip clipPolygon, TSubject subjectPolygon)
        {
            this.clipPolygon = clipPolygon;
            this.subjectPolygon = subjectPolygon;
        }

        /// <summary>
        /// Determines the minimum buffer size required to clip the two polygons.
        /// </summary>
        /// <returns>The minimum buffer size required for <see cref="clipPolygon"/> to clip <see cref="subjectPolygon"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetClipBufferSize()
        {
            // There can only be at most two intersections for each of the subject's vertices
            return subjectPolygon.GetVertices().Length * 2;
        }

        /// <summary>
        /// Clips <see cref="subjectPolygon"/> by <see cref="clipPolygon"/>.
        /// </summary>
        /// <returns>A clockwise-ordered set of vertices representing the result of clipping <see cref="subjectPolygon"/> by <see cref="clipPolygon"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<Vector2> Clip() => Clip(new Vector2[GetClipBufferSize()]);

        /// <summary>
        /// Clips <see cref="subjectPolygon"/> by <see cref="clipPolygon"/> using an intermediate buffer.
        /// </summary>
        /// <param name="buffer">The buffer to contain the clipped vertices. Must have a length of <see cref="GetClipBufferSize"/>.</param>
        /// <returns>A clockwise-ordered set of vertices representing the result of clipping <see cref="subjectPolygon"/> by <see cref="clipPolygon"/>.</returns>
        public Span<Vector2> Clip(Span<Vector2> buffer)
        {
            if (buffer.Length < GetClipBufferSize())
            {
                throw new ArgumentException($"Clip buffer must have a length of {GetClipBufferSize()}, but was {buffer.Length}."
                                            + "Use GetClipBufferSize() to calculate the size of the buffer.", nameof(buffer));
            }

            ReadOnlySpan<Vector2> subjectVertices = subjectPolygon.GetVertices();
            ReadOnlySpan<Vector2> clipVertices = clipPolygon.GetVertices();

            // Buffer is initially filled with the all of the subject's vertices
            subjectVertices.CopyTo(buffer);

            // Make sure that the subject vertices are ordered clockwise
            if (Vector2Extensions.GetRotation(subjectVertices) < 0)
                buffer.Slice(0, subjectVertices.Length).Reverse();

            // Number of vertices in the buffer that need to be tested against
            // This becomes the number of vertices in the resulting polygon after each clipping iteration
            int inputCount = subjectVertices.Length;

            // It's unnecessary to construct + store all the clip edges in a separate array, so only the direction is checked
            if (Vector2Extensions.GetRotation(clipVertices) >= 0)
            {
                // Process the clip edge connecting the last vertex to the first vertex
                inputCount = processClipEdge(new Line(clipVertices[clipVertices.Length - 1], clipVertices[0]), buffer, inputCount);

                // Process all other edges
                for (int c = 1; c < clipVertices.Length; c++)
                {
                    if (inputCount == 0)
                        break;

                    inputCount = processClipEdge(new Line(clipVertices[c - 1], clipVertices[c]), buffer, inputCount);
                }
            }
            else
            {
                // Process the clip edge connecting the last vertex to the first vertex
                inputCount = processClipEdge(new Line(clipVertices[0], clipVertices[clipVertices.Length - 1]), buffer, inputCount);

                // Process all other edges
                for (int c = clipVertices.Length - 1; c > 0; c--)
                {
                    if (inputCount == 0)
                        break;

                    inputCount = processClipEdge(new Line(clipVertices[c], clipVertices[c - 1]), buffer, inputCount);
                }
            }

            return buffer.Slice(0, inputCount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int processClipEdge(in Line clipEdge, in Span<Vector2> buffer, in int inputCount)
        {
            // Temporary storage for the vertices from the buffer as the buffer gets altered
            Span<Vector2> inputVertices = stackalloc Vector2[buffer.Length];

            // Store the original vertices (buffer will get altered)
            buffer.CopyTo(inputVertices);

            int outputCount = 0;

            // Process the edge connecting the last vertex with the first vertex
            outputVertices(ref inputVertices[inputCount - 1], ref inputVertices[0], clipEdge, buffer, ref outputCount);

            // Process all other vertices
            for (int i = 1; i < inputCount; i++)
                outputVertices(ref inputVertices[i - 1], ref inputVertices[i], clipEdge, buffer, ref outputCount);

            return outputCount;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void outputVertices(ref Vector2 startVertex, ref Vector2 endVertex, in Line clipEdge, in Span<Vector2> buffer, ref int bufferIndex)
        {
            if (endVertex.InRightHalfPlaneOf(clipEdge))
            {
                if (!startVertex.InRightHalfPlaneOf(clipEdge))
                {
                    clipEdge.TryIntersectWith(ref startVertex, ref endVertex, out var t);
                    buffer[bufferIndex++] = clipEdge.At(t);
                }

                buffer[bufferIndex++] = endVertex;
            }
            else if (startVertex.InRightHalfPlaneOf(clipEdge))
            {
                clipEdge.TryIntersectWith(ref startVertex, ref endVertex, out var t);
                buffer[bufferIndex++] = clipEdge.At(t);
            }
        }
    }
}
