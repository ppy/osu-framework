// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Graphics.Primitives;
using osuTK;

namespace osu.Framework.Extensions.PolygonExtensions
{
    /// <summary>
    /// Todo: Support segment containment and circles.
    /// Todo: Might be overkill, but possibly support convex decomposition?
    /// </summary>
    public static class ConvexPolygonExtensions
    {
        /// <summary>
        /// Determines whether two convex polygons intersect.
        /// </summary>
        /// <param name="first">The first polygon.</param>
        /// <param name="second">The second polygon.</param>
        /// <returns>Whether the two polygons intersect.</returns>
        public static bool Intersects(this IConvexPolygon first, IConvexPolygon second)
        {
            Vector2[][] bothAxes = { first.GetAxes(), second.GetAxes() };
            Vector2[][] bothVertices = { first.Vertices, second.Vertices };

            return intersects(bothAxes, bothVertices);
        }

        private static bool intersects(Vector2[][] bothAxes, Vector2[][] bothVertices)
        {
            foreach (Vector2[] axes in bothAxes)
            {
                foreach (Vector2 axis in axes)
                {
                    ProjectionRange firstRange = new ProjectionRange(axis, bothVertices[0]);
                    ProjectionRange secondRange = new ProjectionRange(axis, bothVertices[1]);

                    if (!firstRange.Overlaps(secondRange))
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Clips a subject <see cref="IConvexPolygon"/> to another <see cref="IConvexPolygon"/>.
        /// </summary>
        /// <param name="subject">The <see cref="IConvexPolygon"/> to clip.</param>
        /// <param name="clip">The <see cref="IConvexPolygon"/> the polygon defining the clip region.</param>
        /// <returns>The clipped <see cref="IConvexPolygon"/>.</returns>
        public static IConvexPolygon ClipTo(this IConvexPolygon subject, IConvexPolygon clip)
        {
            var outputList = new List<Vector2>(subject.GetVerticesClockwise());

            foreach (var ce in clip.GetEdges())
            {
                if (outputList.Count == 0)
                    break;

                var inputList = new List<Vector2>(outputList);
                outputList.Clear();

                var startPoint = inputList.Last();
                foreach (var endPoint in inputList)
                {
                    if (ce.IsInside(endPoint))
                    {
                        if (!ce.IsInside(startPoint))
                            outputList.Add(ce.At(ce.Intersect(new Line(startPoint, endPoint)).distance));
                        outputList.Add(endPoint);
                    }
                    else if (ce.IsInside(startPoint))
                        outputList.Add(ce.At(ce.Intersect(new Line(startPoint, endPoint)).distance));
                    startPoint = endPoint;
                }
            }

            return new SimpleConvexPolygon(outputList.ToArray());
        }

        /// <summary>
        /// Triangulates a <see cref="IConvexPolygon"/> as a triangle fan.
        /// </summary>
        /// <param name="subject">The <see cref="IConvexPolygon"/> to triangulate.</param>
        /// <returns>An enumeration of the <see cref="Triangle"/>s in the fan.</returns>
        public static IEnumerable<Triangle> Triangulate(this IConvexPolygon subject)
        {
            for (int i = 2; i < subject.Vertices.Length; i++)
                yield return new Triangle(subject.Vertices[0], subject.Vertices[i - 1], subject.Vertices[i]);
        }

        /// <summary>
        /// Computes the total area of a <see cref="IConvexPolygon"/>.
        /// </summary>
        /// <param name="subject">The <see cref="IConvexPolygon"/> to compute the area for.</param>
        /// <returns>The area.</returns>
        public static float Area(this IConvexPolygon subject) => subject.Triangulate().Sum(t => t.Area);

        /// <summary>
        /// Determines whether a <see cref="IConvexPolygon"/> contains a point.
        /// </summary>
        /// <param name="subject">The <see cref="IConvexPolygon"/> to test against.</param>
        /// <param name="pos">The point.</param>
        /// <returns>Whether <paramref name="subject"/> contains <paramref name="pos"/>.</returns>
        public static bool Contains(this IConvexPolygon subject, Vector2 pos) => subject.Triangulate().Any(t => t.Contains(pos));
    }
}
