// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.CompilerServices;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Primitives;
using osuTK;

namespace osu.Framework.Utils
{
    public readonly ref struct ConvexPolygonClipper<TClip, TSubject>
        where TClip : IConvexPolygon
        where TSubject : IConvexPolygon
    {
        private readonly TClip clipPolygon;
        private readonly TSubject subjectPolygon;

        public ConvexPolygonClipper(ref TClip clipPolygon, ref TSubject subjectPolygon)
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
            // Assume every line can intersect every other line.
            // This clipper cannot handle concavity, however this allows for edge cases to be handled gracefully.
            return subjectPolygon.GetVertices().Length * clipPolygon.GetVertices().Length;
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
        public Span<Vector2> Clip(in Span<Vector2> buffer)
        {
            if (buffer.Length < GetClipBufferSize())
            {
                throw new ArgumentException($"Clip buffer must have a length of {GetClipBufferSize()}, but was {buffer.Length}."
                                            + "Use GetClipBufferSize() to calculate the size of the buffer.", nameof(buffer));
            }

            ReadOnlySpan<Vector2> origSubjectVertices = subjectPolygon.GetVertices();
            if (origSubjectVertices.Length == 0)
                return Span<Vector2>.Empty;

            ReadOnlySpan<Vector2> origClipVertices = clipPolygon.GetVertices();
            if (origClipVertices.Length == 0)
                return Span<Vector2>.Empty;

            // Add the subject vertices to the buffer and immediately normalise them
            Span<Vector2> subjectVertices = getNormalised(origSubjectVertices, buffer.Slice(0, origSubjectVertices.Length), true);

            // Since the clip vertices aren't modified, we can use them as they are if they are normalised
            // However if they are not normalised, then we must add them to the buffer and normalise them there
            bool clipNormalised = Vector2Extensions.GetOrientation(origClipVertices) >= 0;
            Span<Vector2> clipBuffer = clipNormalised ? null : stackalloc Vector2[origClipVertices.Length];
            ReadOnlySpan<Vector2> clipVertices = clipNormalised
                ? origClipVertices
                : getNormalised(origClipVertices, clipBuffer, false);

            // Number of vertices in the buffer that need to be tested against
            // This becomes the number of vertices in the resulting polygon after each clipping iteration
            int inputCount = subjectVertices.Length;
            int validClipEdges = 0;

            // Process the clip edge connecting the last vertex to the first vertex
            inputCount = processClipEdge(new Line(clipVertices[^1], clipVertices[0]), buffer, inputCount, ref validClipEdges);

            // Process all other edges
            for (int c = 1; c < clipVertices.Length; c++)
            {
                if (inputCount == 0)
                    break;

                inputCount = processClipEdge(new Line(clipVertices[c - 1], clipVertices[c]), buffer, inputCount, ref validClipEdges);
            }

            if (validClipEdges < 3)
                return Span<Vector2>.Empty;

            return buffer.Slice(0, inputCount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int processClipEdge(in Line clipEdge, in Span<Vector2> buffer, in int inputCount, ref int validClipEdges)
        {
            if (clipEdge.EndPoint == clipEdge.StartPoint)
                return inputCount;

            validClipEdges++;

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
                    clipEdge.TryIntersectWith(ref startVertex, ref endVertex, out float t);
                    buffer[bufferIndex++] = clipEdge.At(t);
                }

                buffer[bufferIndex++] = endVertex;
            }
            else if (startVertex.InRightHalfPlaneOf(clipEdge))
            {
                clipEdge.TryIntersectWith(ref startVertex, ref endVertex, out float t);
                buffer[bufferIndex++] = clipEdge.At(t);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Span<Vector2> getNormalised(in ReadOnlySpan<Vector2> original, in Span<Vector2> bufferSlice, bool verify)
        {
            original.CopyTo(bufferSlice);

            if (!verify || Vector2Extensions.GetOrientation(original) < 0)
                bufferSlice.Reverse();

            return bufferSlice;
        }
    }
}
