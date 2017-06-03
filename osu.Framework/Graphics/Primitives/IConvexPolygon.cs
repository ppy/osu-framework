// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;

namespace osu.Framework.Graphics.Primitives
{
    public interface IConvexPolygon : IPolygon
    {
    }

    public static class ConvexPolygonExtensions
    {
        /// <summary>
        /// Checks if this convex polygon intersects with another convex polygon.
        /// </summary>
        /// <param name="first">Ourselves.</param>
        /// <param name="second">The convex polygon to check. This polygon is not modified.</param>
        /// <returns>Whether this polygon intersects with <paramref name="second"/>.</returns>
        public static bool Intersects(this IConvexPolygon first, ref IConvexPolygon second)
        {
            // Check along the first polygon's axes
            for (int a = 0; a < first.AxisCount; a++)
            {
                Vector2 axis = first.GetAxis(a).Normal;

                float minFirst, maxFirst, minSecond, maxSecond;
                projectionRange(ref axis, ref first, out minFirst, out maxFirst);
                projectionRange(ref axis, ref second, out minSecond, out maxSecond);

                if (minFirst > maxSecond || maxFirst < minSecond)
                    return false;
            }

            // Check along the second polygon's axes
            for (int a = 0; a < second.AxisCount; a++)
            {
                Vector2 axis = second.GetAxis(a).Normal;

                float minFirst, maxFirst, minSecond, maxSecond;
                projectionRange(ref axis, ref first, out minFirst, out maxFirst);
                projectionRange(ref axis, ref second, out minSecond, out maxSecond);

                if (minFirst > maxSecond || maxFirst < minSecond)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Checks if this convex polygon intersects with another convex polygon.
        /// </summary>
        /// <param name="first">Ourselves.</param>
        /// <param name="second">The convex polygon to check.</param>
        /// <returns>Whether this polygon intersects with <paramref name="second"/>.</returns>
        public static bool Intersects(this IConvexPolygon first, IConvexPolygon second)
        {
            return first.Intersects(ref second);
        }

        private static void projectionRange(ref Vector2 axis, ref IConvexPolygon polygon, out float min, out float max)
        {
            min = float.MaxValue;
            max = float.MinValue;

            for (int v = 0; v < polygon.VertexCount; v++)
            {
                float val = Vector2.Dot(axis, polygon.GetVertex(v));
                if (val < min)
                    min = val;
                if (val > max)
                    max = val;
            }
        }
    }
}
