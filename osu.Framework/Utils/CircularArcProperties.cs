// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osuTK;

namespace osu.Framework.Utils
{
    public readonly struct CircularArcProperties
    {
        public readonly bool IsValid;
        public readonly double ThetaStart;
        public readonly double ThetaRange;
        public readonly double Direction;
        public readonly float Radius;
        public readonly Vector2 Centre;

        public double ThetaEnd => ThetaStart + ThetaRange * Direction;

        public CircularArcProperties(double thetaStart, double thetaRange, double direction, float radius, Vector2 centre)
        {
            IsValid = true;
            ThetaStart = thetaStart;
            ThetaRange = thetaRange;
            Direction = direction;
            Radius = radius;
            Centre = centre;
        }

        /// <summary>
        /// Computes various properties that can be used to approximate the circular arc.
        /// </summary>
        /// <param name="controlPoints">Three distinct points on the arc.</param>
        public CircularArcProperties(ReadOnlySpan<Vector2> controlPoints)
        {
            Vector2 a = controlPoints[0];
            Vector2 b = controlPoints[1];
            Vector2 c = controlPoints[2];

            // If we have a degenerate triangle where a side-length is almost zero, then give up and fallback to a more numerically stable method.
            if (Precision.AlmostEquals(0, (b.Y - a.Y) * (c.X - a.X) - (b.X - a.X) * (c.Y - a.Y)))
            {
                IsValid = false;
                ThetaStart = default;
                ThetaRange = default;
                Direction = default;
                Radius = default;
                Centre = default;
                return;
            }

            // See: https://en.wikipedia.org/wiki/Circumscribed_circle#Cartesian_coordinates_2
            float d = 2 * (a.X * (b - c).Y + b.X * (c - a).Y + c.X * (a - b).Y);
            float aSq = a.LengthSquared;
            float bSq = b.LengthSquared;
            float cSq = c.LengthSquared;

            Centre = new Vector2(
                aSq * (b - c).Y + bSq * (c - a).Y + cSq * (a - b).Y,
                aSq * (c - b).X + bSq * (a - c).X + cSq * (b - a).X) / d;

            Vector2 dA = a - Centre;
            Vector2 dC = c - Centre;

            Radius = dA.Length;

            ThetaStart = Math.Atan2(dA.Y, dA.X);
            double thetaEnd = Math.Atan2(dC.Y, dC.X);

            while (thetaEnd < ThetaStart)
                thetaEnd += 2 * Math.PI;

            Direction = 1;
            ThetaRange = thetaEnd - ThetaStart;

            // Decide in which direction to draw the circle, depending on which side of
            // AC B lies.
            Vector2 orthoAtoC = c - a;
            orthoAtoC = new Vector2(orthoAtoC.Y, -orthoAtoC.X);

            if (Vector2.Dot(orthoAtoC, b - a) < 0)
            {
                Direction = -Direction;
                ThetaRange = 2 * Math.PI - ThetaRange;
            }

            IsValid = true;
        }
    }
}
