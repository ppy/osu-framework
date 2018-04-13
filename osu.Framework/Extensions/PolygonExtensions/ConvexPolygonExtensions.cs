// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.Primitives;
using OpenTK;

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
    }
}
