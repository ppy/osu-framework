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
        public static bool Intersects(this IConvexPolygon first, IConvexPolygon second)
        {
            // Check along the first polygon's axes
            for (int a = 0; a < first.AxisCount; a++)
            {
                Vector2 axis = first.GetAxis(a).Normal;

                float minFirst, maxFirst, minSecond, maxSecond;
                projectionRange(axis, first, out minFirst, out maxFirst);
                projectionRange(axis, second, out minSecond, out maxSecond);

                if (minFirst > maxSecond || maxFirst < minSecond)
                    return false;
            }

            // Check along the second polygon's axes
            for (int a = 0; a < second.AxisCount; a++)
            {
                Vector2 axis = second.GetAxis(a).Normal;

                float minFirst, maxFirst, minSecond, maxSecond;
                projectionRange(axis, first, out minFirst, out maxFirst);
                projectionRange(axis, second, out minSecond, out maxSecond);

                if (minFirst > maxSecond || maxFirst < minSecond)
                    return false;
            }

            return true;
        }

        private static void projectionRange(Vector2 axis, IConvexPolygon polygon, out float min, out float max)
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
