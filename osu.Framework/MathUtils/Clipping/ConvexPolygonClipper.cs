// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
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
        public int GetClipBufferSize()
        {
            // There can only be at most two intersections for each of the subject's vertices
            return subjectPolygon.GetVertices().Length * 2;
        }

        /// <summary>
        /// Clips <see cref="subjectPolygon"/> by <see cref="clipPolygon"/>.
        /// </summary>
        /// <returns>A clockwise-ordered set of vertices representing the result of clipping <see cref="subjectPolygon"/> by <see cref="clipPolygon"/>.</returns>
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

            // Make sure that the subject vertices are clockwise-sorted
            Vector2Extensions.ClockwiseSort(buffer.Slice(0, subjectVertices.Length));

            // The edges of clip that the subject will be clipped against
            Span<Line> clipEdges = stackalloc Line[clipVertices.Length];

            // Joins consecutive vertices to form the clip edges
            // This is done via GetRotation() to avoid a secondary temporary storage
            if (Vector2Extensions.GetRotation(clipVertices) < 0)
            {
                for (int i = clipVertices.Length - 1, c = 0; i > 0; i--, c++)
                    clipEdges[c] = new Line(clipVertices[i], clipVertices[i - 1]);
                clipEdges[clipEdges.Length - 1] = new Line(clipVertices[0], clipVertices[clipVertices.Length - 1]);
            }
            else
            {
                for (int i = 0; i < clipVertices.Length - 1; i++)
                    clipEdges[i] = new Line(clipVertices[i], clipVertices[i + 1]);
                clipEdges[clipEdges.Length - 1] = new Line(clipVertices[clipVertices.Length - 1], clipVertices[0]);
            }

            // Number of vertices in the buffer that need to be tested against
            // This becomes the number of vertices in the resulting polygon after each clipping iteration
            int inputCount = subjectVertices.Length;

            // Temporary storage for the vertices from the buffer as the buffer gets altered
            Span<Vector2> inputVertices = stackalloc Vector2[buffer.Length];

            foreach (var ce in clipEdges)
            {
                if (inputCount == 0)
                    break;

                // Store the original vertices (buffer will get altered)
                buffer.CopyTo(inputVertices);

                int outputCount = 0;
                var startPoint = inputVertices[inputCount - 1];

                for (int i = 0; i < inputCount; i++)
                {
                    var endPoint = inputVertices[i];

                    if (endPoint.InRightHalfPlaneOf(ce))
                    {
                        if (!startPoint.InRightHalfPlaneOf(ce))
                            buffer[outputCount++] = ce.At(ce.IntersectWith(new Line(startPoint, endPoint)).distance);

                        buffer[outputCount++] = endPoint;
                    }
                    else if (startPoint.InRightHalfPlaneOf(ce))
                        buffer[outputCount++] = ce.At(ce.IntersectWith(new Line(startPoint, endPoint)).distance);

                    startPoint = endPoint;
                }

                inputCount = outputCount;
            }

            return buffer.Slice(0, inputCount);
        }
    }
}
