// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Drawing;
using osu.Framework.Graphics.Primitives;

namespace osu.Framework.Extensions.RectangleExtensions
{
    public static class RectangleExtensions
    {
        /// <summary>
        /// Checks if this rectangle intersects with a convex polygon.
        /// </summary>
        /// <param name="rectangle">Ourselves.</param>
        /// <param name="polygon">The convex polygon to check.</param>
        /// <returns>Whether this polygon intersects with <paramref name="polygon"/>.</returns>
        public static bool Intersects(this Rectangle rectangle, IConvexPolygon polygon)
        {
            return polygon.Intersects(Quad.FromRectangle(rectangle));
        }
    }
}
